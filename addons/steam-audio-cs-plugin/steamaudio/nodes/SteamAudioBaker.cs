using Godot;
using SteamAudioDotnet.scripts.steamaudio.extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamAudio;
using Vector3 = Godot.Vector3;
using SteamAudioDotnet.scripts.nativelib;
using Error = SteamAudio.Error;
using System.Diagnostics;
using SteamAudioDotnet.scripts.steamaudio.encapsulation;
using SteamAudioDotnet.scripts.steamaudio.resource;
using System.Runtime.InteropServices;

namespace SteamAudioDotnet.scripts.steamaudio.nodes
{
    [Tool, GlobalClass, Icon("res://addons/steam-audio-cs-plugin/materials/icons/Bake.svg")]
    public partial class SteamAudioBaker : Node3D
    {
        // Baker Settings
        [Export, ExportCategory("Baker Settings")]
        public bool Enabled { get; set; } = true;
        [Export]
        public Vector3 BakedAreaExtents { get; set; } = Vector3.One;
        [Export]
        public bool AutoInitialize = true;
        [Export(hint:PropertyHint.SaveFile, hintString:"*.tres,*.res")]
        public string BakeResultsSavePath { get; set; } = string.Empty;
        [Export]
        public bool BakeStaticSources { get; set; } = true;
        [Export]
        public bool BakePathing { get; set; } = true;
        [Export]
        public float StaticSourceInfluenceRadius { get; set; } = 60.0f;

        // Probe Settings
        [Export, ExportCategory("Probe Generation Settings")]
        public float ProbeSpacing { get; set; } = 2.0f;
        [Export]
        public float ProbeHeight { get; set; } = 1.5f;
        [Export]
        public ProbeGenerationType GenerationType { get; set; } = ProbeGenerationType.UniformFloor;

        // Simulator Settings
        [Export, ExportCategory("Simulator Settings")]
        public int NumRays = 32768;
        [Export]
        public ReflectionsBakeFlags ReflectionTypeFlags = ReflectionsBakeFlags.BakeConvolution;
        [Export]
        public SceneType SimulatorRenderer = SceneType.Embree;
        [Export]
        public int NumDiffuseSamples = 1024;
        [Export]
        public int Bounces = 64;
        [Export]
        public float SimulatedDuration = 2.0f;
        [Export]
        public float SavedDuration = 2.0f;
        [Export]
        public int Order = 2;
        [Export]
        public float IrradianceMinDistance = 1.0f;

        // Pathing Settings
        [Export, ExportCategory("Pathing Settings")]
        public float PathingVisibilityRange { get; set; } = 50.0f;
        [Export]
        public float PathingMaxRange { get; set; } = 100.0f;
        [Export]
        public float PathingRadius { get; set; } = 1.0f;
        [Export]
        public float PathingThreshold { get; set; } = 0.1f;

        // Probe Data
        [Export, ExportCategory("Probe Data")]
        public float[] ProbeData { get; set; } = { };
        
        // Internal Simulation Settings
        public static int NumThreads = 8;
        public static int BakeBatchSize = 1;
        public static int RayBatchSize = 16;

        /// <summary>
        /// This will be a nullptr if there is no pathing information loaded.
        /// </summary>
        internal nint PathingProbeBatchPtr = nint.Zero;

        private Control? BakerPopup = null;

        private volatile static SteamAudioBaker? Singleton = null;

        private static ProgressCallback? progressCallback;
        private Stopwatch stopwatch = new Stopwatch();

        private volatile bool isBakeRunning = false;
        private volatile bool isBakeCancelled = false;
        private volatile bool isCurrentTaskPathing = false;
        private volatile int TotalTasks = 0;
        private volatile int CurrentTaskIndex = 0;

        private volatile FmodSteamAudioBridge? currentSteamAudioInstance = null;

        private volatile List<nint> staticMeshes = new();

        // Unmanaged pointers
        private nint probeBatchPtr = nint.Zero;

        ~SteamAudioBaker()
        {
            if (probeBatchPtr != nint.Zero)
            {
                API.iplProbeBatchRelease(ref probeBatchPtr);
                probeBatchPtr = nint.Zero;
            }
        }

        public SteamAudioBaker()
        {
            if (progressCallback == null)
                progressCallback = ProgressReport;
        }

        // Getters for gdscript
        public static SteamAudioBaker? GetSingleton()
        {
            return Singleton;
        }

        public bool IsBakeRunning()
        {
            return isBakeRunning;
        }

        public override void _Ready()
        {
            if (!Engine.IsEditorHint())
            {
                Visible = false;

                if (AutoInitialize)
                    LoadBakedData();
            }
        }

        private void getBakerPopupScript()
        {
            GDScript popupScript = GD.Load<GDScript>("res://addons/steam-audio-cs-plugin/toolbar/bake_popup.gd");

            Variant node = popupScript.Call("get_singleton");

            if (node.VariantType == Variant.Type.Object)
            {
                Control? control = node.Obj as Control;

                if (control != null)
                {
                    BakerPopup = control;
                }
            }
        }

        /// <summary>
        /// One of the only methods in this class that actually runs in-game.
        /// If Enabled is true, attempts to load the baked data from the specified path into the scene.
        /// </summary>
        internal void LoadBakedData()
        {
            if (!Enabled)
                return;

            FmodSteamAudioBridge? steamAudio = FmodSteamAudioBridge.Singleton;

            if (steamAudio == null || steamAudio.Context == nint.Zero)
            {
                GD.PrintErr("Loading Baked Data: Steam Audio context is not initialized.");
                return;
            }

            if (steamAudio.Simulator == nint.Zero)
            {
                GD.PrintErr("Loading Baked Data: Steam Audio simulator is not initialized.");
                return;
            }

            if (BakeResultsSavePath == string.Empty)
            {
                GD.PrintErr("Loading Baked Data: No bake results path specified.");
                return;
            }

            SteamAudioBakeResults? bakeResults = GD.Load<SteamAudioBakeResults>(BakeResultsSavePath);

            if (bakeResults == null)
            {
                GD.PrintErr("Loading Baked Data: Failed to load bake results from the specified path.");
                return;
            }
            
            foreach (KeyValuePair<SteamAudioBakeIdentifier, byte[]> pair in bakeResults.ProbeBatches)
            {
                SteamAudioBakeIdentifier identifier = pair.Key;
                byte[] data = pair.Value;

                SteamAudioSerializedObject bakedDataObj = new(steamAudio.Context, data);

                Error error = API.iplProbeBatchLoad(steamAudio.Context, bakedDataObj.Ptr, out nint probeBatch);

                if (error != Error.Success)
                {
                    GD.PrintErr($"Loading Baked Data: Failed to load probe batch from baked data. Error code: {error}");
                    return;
                }

                API.iplProbeBatchCommit(probeBatch);

                probeBatchPtr = probeBatch;

                if (identifier.Variation == BakedDataVariation.Dynamic)
                {
                    PathingProbeBatchPtr = probeBatch;
                }

                lock (steamAudio.SteamAudioSimulationLock)
                {
                    API.iplSimulatorAddProbeBatch(steamAudio.Simulator, probeBatch);
                    API.iplSimulatorCommit(steamAudio.Simulator);
                }
            }
            
            steamAudio.AudioBaker = this;
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
        }

        private void AddAllStaticGeometry(Node rootNode, FmodSteamAudioBridge steamAudio)
        {
            // This is for the temporary Steam Audio instance used during probe generation & baking.
            // No need to run in-game.
            if (!Engine.IsEditorHint())
                return;

            foreach (Node node in rootNode.GetChildren())
            {
                if (node is SteamAudioStaticGeometry staticGeo)
                {
                    staticGeo.SetupCollision(steamAudio);

                    if (staticGeo.SteamAudioMeshPtrs.Count > 0)
                    {
                        foreach (IntPtr meshPtr in staticGeo.SteamAudioMeshPtrs)
                            staticMeshes.Add(meshPtr);
                    }
                }

                if (node.GetChildCount() > 0)
                {
                    AddAllStaticGeometry(node, steamAudio);
                }
            }
        }

        private void CleanupAllStaticGeometry(FmodSteamAudioBridge steamAudio)
        {
            if (!Engine.IsEditorHint())
                return;

            if (staticMeshes.Count <= 0)
                return;

            foreach (nint staticMeshPtr in staticMeshes)
            {
                SteamAudioStaticGeometry.CleanupStaticMesh(staticMeshPtr, steamAudio.Scene);
            }
        }

        public void GenProbes()
        {
            if (isBakeRunning)
            {
                GD.PrintErr("Probe Generation: A bake is currently running. Please wait for it to finish before generating probes.");
                return;
            }

            FmodSteamAudioBridge steamAudio = new();
            steamAudio.InitSteamAudio(true);

            AddAllStaticGeometry(GetTree().Root, steamAudio);

            try
            {
                if (steamAudio == null || steamAudio.Context == nint.Zero)
                {
                    GD.PrintErr("Probe Generation: Steam Audio context is not initialized.");
                    return;
                }

                if (steamAudio.Scene == nint.Zero)
                {
                    GD.PrintErr("Probe Generation: Steam Audio scene is not initialized.");
                    return;
                }

                GD.Print("Generating Steam Audio Probes...");

                API.iplSceneCommit(steamAudio.Scene);
                API.iplSimulatorCommit(steamAudio.Simulator);

                Transform3D boxTransform = Transform3D.Identity;
                boxTransform = boxTransform.Scaled(BakedAreaExtents * 2);
                boxTransform.Origin = GlobalTransform.Origin;

                Matrix4x4 boxMatrix = boxTransform.AsSteamAudioMatrix();

                ProbeGenerationParams probeGenParams = new ProbeGenerationParams()
                {
                    type = GenerationType,
                    spacing = ProbeSpacing,
                    height = ProbeHeight,
                    transform = boxMatrix
                };

                Error error = API.iplProbeArrayCreate(steamAudio.Context, out nint probeArray);

                if (error != Error.Success || probeArray == nint.Zero)
                {
                    GD.PrintErr($"Probe Generation: Failed to create probe array. Error code: {error}");
                    return;
                }

                API.iplProbeArrayGenerateProbes(probeArray, steamAudio.Scene, ref probeGenParams);

                int probeCount = API.iplProbeArrayGetNumProbes(probeArray);

                ProbeData = new float[probeCount * 4]; // Each probe has 4 float values (x, y, z, radius)

                for (int i = 0; i < probeCount; i++)
                {
                    Sphere probe = API.iplProbeArrayGetProbe(probeArray, i);

                    ProbeData[i * 4 + 0] = probe.center.x;
                    ProbeData[i * 4 + 1] = probe.center.y;
                    ProbeData[i * 4 + 2] = probe.center.z;
                    ProbeData[i * 4 + 3] = probe.radius;
                }

                UpdateGizmos();

                GD.Print("Steam Audio Probes Generated Successfully!");
                GD.Print($"Total Probes Generated: {probeCount}");
            }
            finally
            {
                CleanupAllStaticGeometry(steamAudio);

                steamAudio.Shutdown();
                steamAudio = null; // I know this is technically a nullability warning, but I feel like this makes sense
                                   // so that it gets collected.

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// Returns a list of the positions of all static sources in the level.
        /// </summary>
        /// <param name="rootNode"></param>
        /// <returns></returns>
        private static void CollectStaticSources(List<Vector3> sourcePositions, Node rootNode)
        {
            foreach (Node child in rootNode.GetChildren())
            {
                if (child is SteamAudioStaticSourceMarker marker)
                {
                    sourcePositions.Add(marker.GlobalTransform.Origin);
                }

                if (child.GetChildCount() > 0)
                {
                    CollectStaticSources(sourcePositions, child);
                }
            }
        }

        private nint CreateBlankProbeBatch(FmodSteamAudioBridge steamAudio, Sphere[] probeData)
        {
            Error error = API.iplProbeBatchCreate(steamAudio.Context, out nint probeBatch);

            if (error != Error.Success)
            {
                GD.PrintErr($"BAKE FAILED: Failed to create a probe batch! Error: {error}");
                return nint.Zero;
            }

            for (int i = 0; i < probeData.Length; i++)
            {
                API.iplProbeBatchAddProbe(probeBatch, probeData[i]);
            }

            API.iplProbeBatchCommit(probeBatch);

            return probeBatch;
        }

        private PathBakeParams GetBakePathingParams(FmodSteamAudioBridge steamAudio)
        {
            return new()
            {
                scene = steamAudio.Scene,
                numSamples = 1,
                visRange = PathingVisibilityRange,
                pathRange = PathingMaxRange,
                numThreads = NumThreads,
                radius = PathingRadius,
                threshold = PathingThreshold
            };
        }

        private ReflectionsBakeParams GetBakeParams(FmodSteamAudioBridge steamAudio)
        {
            return new()
            {
                scene = steamAudio.Scene,
                sceneType = SimulatorRenderer,
                bakeFlags = ReflectionTypeFlags,
                numRays = NumRays,
                numDiffuseSamples = NumDiffuseSamples,
                numBounces = Bounces,
                simulatedDuration = SimulatedDuration,
                savedDuration = SavedDuration,
                order = Order,
                numThreads = NumThreads,
                irradianceMinDistance = IrradianceMinDistance,
                bakeBatchSize = BakeBatchSize,
                openCLDevice = steamAudio.OpenCLDevicePointer,
                radeonRaysDevice = steamAudio.RadeonRaysDevicePointer,
                rayBatchSize = RayBatchSize
            };
        }

        public void Bake()
        {
            getBakerPopupScript();

            FmodSteamAudioBridge steamAudio = new();
            steamAudio.InitSteamAudio(true);

            currentSteamAudioInstance = steamAudio;

            staticMeshes.Clear();
            AddAllStaticGeometry(GetTree().Root, steamAudio);

            if (steamAudio == null || steamAudio.Context == nint.Zero)
            {
                GD.PrintErr("Baking: Steam Audio context is not initialized.");
                return;
            }

            if (steamAudio.Scene == nint.Zero)
            {
                GD.PrintErr("Baking: Steam Audio scene is not initialized.");
                return;
            }

            if (ProbeData.Length == 0)
            {
                GD.PrintErr("Baking: No probe data available. Generate probes before baking.");
                return;
            }

            if (ProbeData.Length % 4 != 0)
            {
                GD.PrintErr("Baking: Invalid probe data length.");
                return;
            }

            if (BakeResultsSavePath == string.Empty)
            {
                GD.PrintErr("Baking: Please specify a save path for the bake results.");
                return;
            }

            if (!DirAccess.DirExistsAbsolute(BakeResultsSavePath.GetBaseDir()))
            {
                GD.PrintErr("Baking: The specified save directory does not exist.");
                return;
            }

            API.iplSceneCommit(steamAudio.Scene);
            API.iplSimulatorCommit(steamAudio.Simulator);

            if (steamAudio.RadeonRaysDevicePointer == nint.Zero && steamAudio.SceneType == SceneType.RadeonRays)
            {
                GD.PrintErr("Baking: Couldn't find Radeon Rays device.");
                return;
            }

            if (steamAudio.OpenCLDevicePointer == nint.Zero && steamAudio.SceneType == SceneType.RadeonRays)
            {
                GD.PrintErr("Baking: Couldn't find OpenCL device. Consider switching from Radeon Rays to Embree or Default.");
                return;
            }

            GD.Print($"BAKE PROGRESS: Loading probe data...");

            int numProbes = ProbeData.Length / 4;

            Sphere[] probeData = new Sphere[numProbes];

            for (int i = 0; i < numProbes; i++)
            {
                probeData[i].center.x = ProbeData[(i * 4) + 0];
                probeData[i].center.y = ProbeData[(i * 4) + 1];
                probeData[i].center.z = ProbeData[(i * 4) + 2];
                probeData[i].radius = ProbeData[(i * 4) + 3];
            }

            // Add all static sources to be baked seperately.
            List<Vector3> sourcePositions = [];
            CollectStaticSources(sourcePositions, GetTree().Root);

            Queue<BakeTask> bakeTasks = new();

            // Bake general reverb for dynamic sources.
            {
                nint probeBatch = CreateBlankProbeBatch(steamAudio, probeData);

                if (probeBatch == nint.Zero)
                {
                    GD.PrintErr("BAKE FAILED: Failed to create probe batch for reverb bake task.");
                    return;
                }

                ReflectionsBakeParams bakeParams = GetBakeParams(steamAudio);

                bakeParams.probeBatch = probeBatch;

                bakeParams.identifier.type = BakedDataType.Reflections;
                bakeParams.identifier.variation = BakedDataVariation.Reverb;

                bakeTasks.Enqueue(new BakeTask(bakeParams));
            }

            // Run individual passes for static sources.
            if (BakeStaticSources)
            {
                foreach (Vector3 sourcePos in sourcePositions)
                {
                    nint probeBatch = CreateBlankProbeBatch(steamAudio, probeData);

                    if (probeBatch == nint.Zero)
                    {
                        GD.PrintErr("BAKE FAILED: Failed to create probe batch for static source bake task.");
                        return;
                    }

                    ReflectionsBakeParams bakeParams = GetBakeParams(steamAudio);

                    bakeParams.probeBatch = probeBatch;

                    bakeParams.identifier.type = BakedDataType.Reflections;
                    bakeParams.identifier.variation = BakedDataVariation.StaticSource;

                    bakeParams.identifier.endpointInfluence.center.x = sourcePos.X;
                    bakeParams.identifier.endpointInfluence.center.y = sourcePos.Y;
                    bakeParams.identifier.endpointInfluence.center.z = sourcePos.Z;
                    bakeParams.identifier.endpointInfluence.radius = StaticSourceInfluenceRadius;

                    bakeTasks.Enqueue(new BakeTask(bakeParams));
                }
            }

            // Bake pathing
            if (BakePathing)
            {
                nint probeBatch = CreateBlankProbeBatch(steamAudio, probeData);

                if (probeBatch == nint.Zero)
                {
                    GD.PrintErr("BAKE FAILED: Failed to create probe batch for static source bake task.");
                    return;
                }

                PathBakeParams bakeParams = GetBakePathingParams(steamAudio);

                bakeParams.probeBatch = probeBatch;

                bakeParams.identifier.type = BakedDataType.Pathing;
                bakeParams.identifier.variation = BakedDataVariation.Dynamic;

                bakeTasks.Enqueue(new BakeTask(bakeParams));
            }

            GD.Print("Steam Audio: Starting bake...");
            isBakeRunning = true;
            Singleton = this;

            BakerPopup?.Call("set_button_label", "Cancel Bake");

            Task.Run(() => RunBakeTask(steamAudio, bakeTasks, BakeResultsSavePath));
        }

        public async Task RunBakeTask(FmodSteamAudioBridge steamAudio, Queue<BakeTask> bakeTasks, string savePath)
        {
            try
            {
                if (progressCallback == null)
                {
                    Callable.From(() => { GD.PrintErr("BAKE FAILED: Couldn't obtain the progress callback delegate."); })
                        .CallDeferred();
                    return;
                }

                stopwatch.Restart();

                if (BakerPopup != null)
                {
                    BakerPopup.CallDeferred("set_popup_visible", true);
                    GD.Print("SET POPUP VISIBLE!");
                }

                SteamAudioBakeResults results = new(ReflectionTypeFlags);

                TotalTasks = bakeTasks.Count;
                CurrentTaskIndex = 0;

                while (bakeTasks.Count > 0)
                {
                    if (isBakeCancelled)
                    {
                        break;
                    }

                    BakeTask currentTask = bakeTasks.Dequeue();

                    if (currentTask.Type == BakeType.Pathing)
                    {
                        isCurrentTaskPathing = true;
                    }
                    else
                    {
                        isCurrentTaskPathing = false;
                    }

                    BakedDataIdentifier? bakeIdentifier = null;

                    if (currentTask.Type == BakeType.Reverb)
                    {
                        bakeIdentifier = currentTask.BakeParams.identifier;
                    }
                    else if (currentTask.Type == BakeType.Pathing)
                    {
                        bakeIdentifier = currentTask.PathBakeParams.identifier;
                    }

                    if (BakerPopup != null)
                    {
                        string bakeType = string.Empty;

                        switch (bakeIdentifier?.variation)
                        {
                            case BakedDataVariation.Reverb:
                                bakeType = "Reverb";
                                break;
                            case BakedDataVariation.StaticSource:
                                bakeType = "Static Source";
                                break;
                            case BakedDataVariation.Dynamic:
                                bakeType = "Pathing";
                                break;
                            default:
                                bakeType = "Unknown";
                                break;
                        }

                        BakerPopup.CallDeferred("set_progress_label",
                            $"Task {CurrentTaskIndex + 1}/{TotalTasks} - {bakeType}");
                    }

                    switch (currentTask.Type)
                    {
                        // REVERB
                        case BakeType.Reverb:
                            {
                                API.iplReflectionsBakerBake(
                                    steamAudio.Context,
                                    ref currentTask.BakeParams,
                                    progressCallback,
                                    nint.Zero);

                                SteamAudioSerializedObject serializedObj = new(steamAudio.Context);

                                API.iplProbeBatchSave(currentTask.BakeParams.probeBatch, serializedObj.Ptr);

                                byte[] data = new byte[(int)serializedObj.Size];

                                Marshal.Copy(serializedObj.Data, data, 0, (int)serializedObj.Size);

                                Sphere endpointInfluence = currentTask.BakeParams.identifier.endpointInfluence;

                                SteamAudioBakeIdentifier identifier = new()
                                {
                                    Variation = currentTask.BakeParams.identifier.variation,
                                    Origin = new(endpointInfluence.center.x, endpointInfluence.center.y, endpointInfluence.center.z),
                                    Radius = endpointInfluence.radius
                                };

                                results.ProbeBatches.Add(identifier, data);

                                API.iplProbeBatchRelease(ref currentTask.BakeParams.probeBatch);
                            }
                            break;

                        // PATHING
                        case BakeType.Pathing:
                            {
                                API.iplPathBakerBake(
                                    steamAudio.Context,
                                    ref currentTask.PathBakeParams,
                                    progressCallback,
                                    nint.Zero);

                                SteamAudioSerializedObject serializedObj = new(steamAudio.Context);

                                API.iplProbeBatchSave(currentTask.PathBakeParams.probeBatch, serializedObj.Ptr);

                                byte[] data = new byte[(int)serializedObj.Size];

                                Marshal.Copy(serializedObj.Data, data, 0, (int)serializedObj.Size);

                                Sphere endpointInfluence = currentTask.PathBakeParams.identifier.endpointInfluence;

                                SteamAudioBakeIdentifier identifier = new()
                                {
                                    Variation = currentTask.PathBakeParams.identifier.variation,
                                    Origin = new(endpointInfluence.center.x, endpointInfluence.center.y, endpointInfluence.center.z),
                                    Radius = endpointInfluence.radius
                                };

                                results.ProbeBatches.Add(identifier, data);

                                API.iplProbeBatchRelease(ref currentTask.PathBakeParams.probeBatch);
                            }
                            break;
                    }

                    CurrentTaskIndex++;
                }

                if (!isBakeCancelled)
                {
                    Godot.Error error = ResourceSaver.Save(results, savePath);

                    if (error != Godot.Error.Ok)
                    {
                        GD.PrintErr($"BAKE FAILED: Failed to save bake results to {savePath}. Godot Error: {error}");
                        return;
                    }
                }
                else
                {
                    BakerPopup?.CallDeferred("set_task_progress", 0);
                    BakerPopup?.CallDeferred("set_general_progress", 0);
                }

                stopwatch.Stop();

                GD.Print($"Bake {(isBakeCancelled ? "cancelled." : "finished.")} " +
                    $"{stopwatch.Elapsed.TotalSeconds:F2} seconds elapsed.");

                BakerPopup?.CallDeferred("set_progress_label",
                        $"{(isBakeCancelled ? "Cancelled" : "Bake finished")} - {stopwatch.Elapsed.TotalSeconds:F2}s elapsed");
            }
            finally
            {
                isBakeRunning = false;
                isBakeCancelled = false;

                BakerPopup?.CallDeferred("set_button_label", "Close Window");

                Callable.From(() =>
                {
                    CleanupAllStaticGeometry(steamAudio);

                    steamAudio.Shutdown();
                    steamAudio = null; // I know this is technically a nullability warning, but I feel like this makes sense
                                       // so that it gets collected.

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }).CallDeferred();

                Singleton = null;
                currentSteamAudioInstance = null;
            }
        }

        public void CancelBake()
        {
            if (!isBakeRunning)
            {
                GD.PrintErr("Couldn't cancel bake, no active tasks found.");
                return;
            }

            if (currentSteamAudioInstance == null)
            {
                GD.PrintErr("Couldn't find current Steam Audio instance.");
                return;
            }

            if (currentSteamAudioInstance.Context == nint.Zero)
            {
                GD.PrintErr("Steam Audio context was null.");
                return;
            }

            isBakeCancelled = true;

            API.iplPathBakerCancelBake(currentSteamAudioInstance.Context);
            API.iplReflectionsBakerCancelBake(currentSteamAudioInstance.Context);
        }

        public void ProgressReport(float progress, nint userdata)
        {
            if (BakerPopup != null)
            {
                float totalProgress = ((float)(CurrentTaskIndex) + progress) / TotalTasks;

                BakerPopup.CallDeferred("set_task_progress", progress * 100);
                BakerPopup.CallDeferred("set_general_progress", totalProgress * 100);
            }

            //Callable.From(() => { GD.Print($"Bake Progress: {(int)(progress * 100)}%"); }).CallDeferred();
        }
    }

    public struct BakeTask
    {
        public ReflectionsBakeParams BakeParams;
        public PathBakeParams PathBakeParams;

        public BakeType Type { get; set; }

        public BakeTask(ReflectionsBakeParams bakeParams)
        {
            BakeParams = bakeParams;
            Type = BakeType.Reverb;
        }

        public BakeTask(PathBakeParams pathParams)
        {
            PathBakeParams = pathParams;
            Type = BakeType.Pathing;
        }
    }

    public enum BakeType
    {
        Reverb,
        Pathing
    }
}

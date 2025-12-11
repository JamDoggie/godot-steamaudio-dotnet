using Godot;
using SteamAudioDotnet.scripts.steamaudio;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Error = SteamAudio.Error;
using Vector3 = Godot.Vector3;
using Thread = System.Threading.Thread;
using FMOD;
using FMOD.Studio;
using SteamAudioDotnet.scripts.steamaudio.encapsulation;
using SteamAudio;
using SteamAudioDotnet.scripts.steamaudio.nodes;
using SteamAudioDotnet.scripts.steamaudio.soundprocessors;

namespace SteamAudioDotnet.scripts.nativelib
{
    [GlobalClass, Tool]
    public partial class FmodSteamAudioBridge : Node3D
    {
        // Create a static instance of the LogCallback delegate so it doesn't get garbage collected.
        private static LogCallback? logCallback = null;

        [Export]
        public Node? FMODGodotBridge = null;
        [Export]
        public Node3D? ListenerNode { get; internal set; } = null;
        
        [Export]
        public int SamplingRate { get; internal set; } = 48000;
        [Export]
        public SceneType SceneType { get; internal set; } = SceneType.Embree;
        [Export]
        public bool UseTrueAudioNext { get; internal set; } = false;
        [Export]
        public int OcclusionSamples { get; internal set; } = 16;
        [Export]
        public int MaxReflectionRays { get; internal set; } = 4096;
        [Export]
        public int DiffuseSamples { get; internal set; } = 1024;
        [Export]
        public int NumThreads { get; internal set; } = 2;
        [Export]
        public int VisSamples { get; internal set; } = 4;
        [Export]
        public float MaxReflectionDuration { get; internal set; } = 2.0f;
        [Export]
        public int MaxReflectionSources { get; internal set; } = 8;
        [Export]
        public bool EnableValidationLayer { get; internal set; } = false;
        [Export]
        public bool SimulatePathing { get; set; } = false;

        internal Dictionary<string, OcclusionProcessor> OcclusionProcessors = new();

        internal bool DebugBaker = true;

        public int FrameSize { get; internal set; } = 512;
        public IntPtr Context = IntPtr.Zero;
        public IntPtr Scene = IntPtr.Zero;
        public IntPtr Simulator = IntPtr.Zero;

        public static FmodSteamAudioBridge? Singleton = null;

        public static object AudioSourcesLock = new object();

        /// <summary>
        /// NOTE: Lock AudioSourcesLock while enumerating this list.
        /// </summary>
        public static List<SteamAudioStaticSourceMarker> StaticAudioSources { get; set; } = new();

        public SteamAudioSource? ReverbListenerSource { get; internal set; } = null;

        private SteamAudioBaker? audioBaker = null;
        public SteamAudioBaker? AudioBaker
        {
            get
            {
                lock (SteamAudioCollectionsLock)
                {
                    return audioBaker;
                }
            }
            internal set
            {
                lock (SteamAudioCollectionsLock)
                {
                    audioBaker = value;
                }
            }
        }

        public bool BakedDataLoaded => AudioBaker != null;

        internal bool SceneCommitQueued = false;

        internal SimulationFlags SimFlags = SimulationFlags.Direct | SimulationFlags.Reflections | SimulationFlags.Pathing;
        internal SimulationFlags SourceFlags = SimulationFlags.Direct | SimulationFlags.Reflections | SimulationFlags.Pathing;

        private List<SteamAudioSource> ActiveSources { get; set; } = new();

        private List<Variant> EventsToAdd = new();
        private List<Variant> EventsToRemove = new();

        public nint OpenCLDevicePointer = IntPtr.Zero;
        public nint RadeonRaysDevicePointer = IntPtr.Zero;
        public nint EmbreeDevicePointer = IntPtr.Zero;
        public nint HRTFPointer = IntPtr.Zero;
        private nint tanDevice = IntPtr.Zero;
        

        private SimulationSharedInputs SharedInputs = new();

        // Lifetime handles. TODO: use objects with destructors instead.
        private nint tanDeviceHandle = IntPtr.Zero;

        // Threading
        private bool simulationThreadRunning = false;
        /// <summary>
        /// If calling any API functions that cannot be run during a simulation, lock this object.
        /// </summary>
        internal volatile object SteamAudioSimulationLock = new();
        /// <summary>
        /// If modifying or iterating any collections used between threads, lock this object.
        /// </summary>
        internal volatile object SteamAudioCollectionsLock = new();

        internal unsafe DeviationModel* DeviationModel = null;

        private void LogCallback(LogLevel level, string s)
        {
            GD.Print($"[Steam Audio] -- {level}: {s.Trim()}");
        }

        // Getters for gdscript access
        public static FmodSteamAudioBridge? GetSingleton()
        {
            return Singleton;
        }

        public static void EditorInitSingleton()
        {
            FmodSteamAudioBridge steamAudioBridge = new();

            GDScript fmodGodotScript = GD.Load<GDScript>("res://scripts/fmod_gdscript_bridge.gd");
            steamAudioBridge.FMODGodotBridge = fmodGodotScript.New().As<Node>();

            // Because we're creating a new instance, all the options will be default.
            // TODO: implement a settings page for Steam Audio settings in the editor.
            steamAudioBridge.InitSteamAudio();
        }

        public static void EditorSceneSwitched(Node sceneRoot)
        {

        }

        public unsafe FmodSteamAudioBridge()
        {
            // This is unused really, it just stops the validation layer from spamming the console.
            DeviationModel = (DeviationModel*)Marshal.AllocHGlobal(sizeof(DeviationModel));
            DeviationModel->type = DeviationModelType.Default;
            DeviationModel->userData = IntPtr.Zero;
            DeviationModel->callback = IntPtr.Zero;
        }
        
        public override void _Ready()
        {
            if (!Engine.IsEditorHint())
            {
                InitSteamAudio();

                Thread simulationThread = new(RunSimulation)
                {
                    IsBackground = true
                };

                simulationThreadRunning = true;

                simulationThread.Start();
            }
        }

        ~FmodSteamAudioBridge()
        {
            Shutdown();
        }

        internal void Shutdown()
        {
            unsafe
            {
                if ((nint)DeviationModel != nint.Zero)
                {
                    Marshal.FreeHGlobal((nint)DeviationModel);
                    DeviationModel = (DeviationModel*)nint.Zero;
                }
            }

            if (OpenCLDevicePointer != IntPtr.Zero)
            {
                API.iplOpenCLDeviceRelease(ref OpenCLDevicePointer);
                OpenCLDevicePointer = IntPtr.Zero;
            }

            if (RadeonRaysDevicePointer != IntPtr.Zero)
            {
                API.iplRadeonRaysDeviceRelease(ref RadeonRaysDevicePointer);
                RadeonRaysDevicePointer = IntPtr.Zero;
            }

            if (tanDeviceHandle != IntPtr.Zero)
            {
                API.iplTrueAudioNextDeviceRelease(ref tanDeviceHandle);
                tanDeviceHandle = IntPtr.Zero;
            }

            if (Simulator != IntPtr.Zero)
            {
                API.iplSimulatorRelease(ref Simulator);
                Simulator = IntPtr.Zero;
            }

            if (tanDeviceHandle != nint.Zero)
            {
                API.iplTrueAudioNextDeviceRelease(ref tanDeviceHandle);
                tanDeviceHandle = nint.Zero;
                tanDevice = IntPtr.Zero;
            }

            if (EmbreeDevicePointer != IntPtr.Zero)
            {
                API.iplEmbreeDeviceRelease(ref EmbreeDevicePointer);
                EmbreeDevicePointer = IntPtr.Zero;
            }

            if (HRTFPointer != IntPtr.Zero)
            {
                API.iplHRTFRelease(ref HRTFPointer);
                HRTFPointer = IntPtr.Zero;
            }

            if (Scene != IntPtr.Zero)
            {
                API.iplSceneRelease(ref Scene);
                Scene = IntPtr.Zero;
            }

            if (Context != IntPtr.Zero)
            {
                API.iplContextRelease(ref Context);
                Context = IntPtr.Zero;
            }

            SteamFmodApi.iplFMODTerminate();
        }

        public float FMODGetParam(string paramName)
        {
            if (FMODGodotBridge == null)
            {
                GD.PrintErr("FmodGodotBridge node not set in FmodSteamAudioBridge!");
                return 0f;
            }

            return FMODGodotBridge.Call("get_param", paramName).AsSingle();
        }

        public void FMODSetParam(string paramName, float value)
        {
            if (FMODGodotBridge == null)
            {
                GD.PrintErr("FmodGodotBridge node not set in FmodSteamAudioBridge!");
                return;
            }

            FMODGodotBridge.Call("set_param", paramName, value);
        }

        public void InitSteamAudio(bool bakingInstance = false)
        {
            // We initialize a bit differently if this instance is for baking.
            if (!bakingInstance)
            {
                if (Singleton != null)
                {
                    GD.PrintErr($"Multiple {GetType().Name} instances detected! There should only be one per scene.");
                    return;
                }

                Singleton = this;
            }
            
            if (bakingInstance)
            {
                FMODGodotBridge = new();
            }

            if (FMODGodotBridge == null)
            {
                GD.PrintErr("FmodGodotBridge node not set in FmodSteamAudioBridge!");
                return;
            }

            // Set settings from fmod
            if (!Engine.IsEditorHint())
                FrameSize = FMODGodotBridge.Call("get_buffer_size").AsInt32();

            // Init steam audio
            ContextSettings contextSettings = new()
            {
                version = Constants.kVersion,
                flags = EnableValidationLayer ? ContextFlags.Validation : 0,
                simdLevel = SIMDLevel.AVX512
            };

            if (!bakingInstance || DebugBaker)
            {
                if (logCallback == null)
                    logCallback = LogCallback;

                contextSettings.logCallback = logCallback;
            }
                
            IntPtr context = IntPtr.Zero;

            Error error = API.iplContextCreate(ref contextSettings, out context);
            #region Context error checking
            if (error != Error.Success)
            {
                GD.PrintErr("Failed to create Steam Audio context: " + error);
                return;
            }

            if (context == IntPtr.Zero)
            {
                GD.PrintErr("Steam Audio context handle is null... somehow.");
                return;
            }
            #endregion

            Context = context;

            AudioSettings audioSettings = new()
            {
                frameSize = FrameSize,
                samplingRate = SamplingRate
            };

            HRTFSettings hrtfSettings = new()
            {
                type = HRTFType.Default,
                volume = 1.0f,
                normType = HRTFNormType.None,
            };

            error = API.iplHRTFCreate(Context, ref audioSettings, ref hrtfSettings, out HRTFPointer);

            #region HRTF error checking
            if (error != Error.Success)
            {
                GD.PrintErr("Failed to create Steam Audio HRTF: " + error);
                return;
            }

            if (HRTFPointer == IntPtr.Zero)
            {
                GD.PrintErr("Steam Audio HRTF handle is null... somehow.");
                return;
            }
            #endregion

            EmbreeDeviceSettings embreeDeviceSettings = new()
            {
                
            };

            error = API.iplEmbreeDeviceCreate(Context, ref embreeDeviceSettings, out EmbreeDevicePointer);

            #region Embree device error checking
            if (error != Error.Success)
            {
                GD.PrintErr("Failed to create Steam Audio Embree Device: " + error);
                return;
            }

            if (EmbreeDevicePointer == IntPtr.Zero)
            {
                GD.PrintErr("Steam Audio Embree Device handle is null... somehow.");
                return;
            }
            #endregion

            SceneSettings sceneSettings = new()
            {
                type = SceneType,
                embreeDevice = EmbreeDevicePointer,
                userData = IntPtr.Zero
            };

            bool openCLSuccess = AttemptOpenCLInit(Context, ref sceneSettings);

            if (openCLSuccess)
            {
                if (SceneType == SceneType.RadeonRays)
                    AttemptRadeonRaysInit(Context, ref sceneSettings);

                if (UseTrueAudioNext)
                    AttemptTrueAudioNextInit();
            }
            else if (SceneType == SceneType.RadeonRays)
            {
                SceneType = SceneType.Embree;
            }

            IntPtr scene = IntPtr.Zero;
            error = API.iplSceneCreate(Context, ref sceneSettings, out scene);

            #region Scene error checking
            if (error != Error.Success)
            {
                GD.PrintErr("Failed to create Steam Audio Scene: " + error);
                return;
            }

            if (scene == IntPtr.Zero)
            {
                GD.PrintErr("Steam Audio Scene handle is null... somehow.");
                return;
            }
            #endregion

            Scene = scene;

            SimulationSettings simSettings = new()
            {
                sceneType = SceneType,
                reflectionType = ReflectionEffectType.Convolution,
                flags = SimFlags,
                maxNumOcclusionSamples = OcclusionSamples,
                maxNumRays = MaxReflectionRays,
                numDiffuseSamples = DiffuseSamples,
                maxDuration = MaxReflectionDuration,
                maxOrder = 2,
                maxNumSources = MaxReflectionSources,
                numThreads = NumThreads,
                rayBatchSize = 16,
                numVisSamples = VisSamples,
                samplingRate = SamplingRate,
                frameSize = FrameSize,
                radeonRaysDevice = RadeonRaysDevicePointer,
                openCLDevice = OpenCLDevicePointer,
                tanDevice = tanDevice,
            };

            if (UseTrueAudioNext && tanDevice != IntPtr.Zero && openCLSuccess)
            {
                simSettings.reflectionType = ReflectionEffectType.TrueAudioNext;
                GD.Print("Steam Audio: Using TrueAudioNext for reflections.");
            }
                
            IntPtr simulator = IntPtr.Zero;
            error = API.iplSimulatorCreate(Context, ref simSettings, out simulator);

            Simulator = simulator;

            #region Simulator error checking
            if (error != Error.Success)
            {
                GD.PrintErr("Failed to create Steam Audio Simulator: " + error);
                return;
            }

            if (Simulator == IntPtr.Zero)
            {
                GD.PrintErr("Steam Audio Simulator handle is null... somehow.");
                return;
            }
            #endregion

            API.iplSimulatorSetScene(Simulator, scene);
            API.iplSimulatorCommit(Simulator);

            // Create a listener source for reverb
            SteamAudioSource listenerSource = new(this);

            if (listenerSource.IsValid)
            {
                ReverbListenerSource = listenerSource;
            }

            // Don't init fmod if we're just running a bake.
            if (!bakingInstance)
            {
                // FMOD integration
                SteamFmodApi.iplFMODInitialize(Context);
                SteamFmodApi.iplFMODSetHRTF(HRTFPointer);
                SteamFmodApi.iplFMODSetSimulationSettings(simSettings);

                SteamFmodApi.iplFMODGetVersion(out uint major, out uint minor, out uint patch);

                GD.Print($"Steam Audio FMOD version {major}.{minor}.{patch}");

                if (listenerSource.IsValid)
                {
                    SteamFmodApi.iplFMODSetReverbSource(listenerSource.Ptr);
                }
            }

            // Final commit
            API.iplSimulatorCommit(Simulator);

            GD.Print("FmodSteamAudioBridge initialized successfully.");
        }

        private void AttemptTrueAudioNextInit()
        {
            if (OpenCLDevicePointer == IntPtr.Zero)
            {
                printErrInfo("OpenCL handle is null; cannot create True Audio Next device.");
                return;
            }

            TrueAudioNextDeviceSettings settings = new()
            {
                frameSize = FrameSize,
                irSize = Mathf.CeilToInt(MaxReflectionDuration * SamplingRate),
                maxSources = MaxReflectionSources,
                order = 1
            };

            Error error = API.iplTrueAudioNextDeviceCreate(OpenCLDevicePointer, ref settings, out tanDevice);

            if (error != Error.Success)
            {
                printErrInfo(error.ToString());
                return;
            }

            tanDeviceHandle = tanDevice;

            void printErrInfo(string message)
            {
                GD.PrintErr("Failed to create Steam Audio OpenCL Device List: " + message);
                GD.PrintErr("Continuing without OpenCL support... (switching to Embree)");
            }
        }

        private void AttemptRadeonRaysInit(IntPtr context, ref SceneSettings settings, SceneType fallbackType = SceneType.Embree)
        {
            if (OpenCLDevicePointer == IntPtr.Zero)
            {
                GD.PrintErr("OpenCL handle is null; cannot create RadeonRays device.");
                settings.type = fallbackType;
                SceneType = fallbackType;
                return;
            }

            RadeonRaysDeviceSettings radeonRaysDeviceSettings = new();

            Error error = API.iplRadeonRaysDeviceCreate(OpenCLDevicePointer, ref radeonRaysDeviceSettings, out nint radeonRaysDevice);

            if (error != Error.Success)
            {
                printErrInfo(error.ToString());
                settings.type = fallbackType;
                SceneType = fallbackType;
                return;
            }

            settings.radeonRaysDevice = radeonRaysDevice;

            RadeonRaysDevicePointer = radeonRaysDevice;

            void printErrInfo(string message)
            {
                GD.PrintErr("Failed to init Radeon Rays: " + message);
                GD.PrintErr("Continuing without OpenCL support... (switching to Embree)");
            }
        }

        private bool AttemptOpenCLInit(IntPtr context, ref SceneSettings settings, SceneType fallbackType = SceneType.Embree)
        {
            // Initialize an OpenCL context
            // https://valvesoftware.github.io/steam-audio/doc/capi/opencl.html#_CPPv4N23IPLOpenCLDeviceSettings22fractionCUsForIRUpdateE
            OpenCLDeviceSettings openClSettings = new()
            {
                Type = OpenCLDeviceType.GPU,
                NumCUsToReserve = 0, // 0 = can use all available CUs.
                FractionCUsForIRUpdate = 0.5f,
            };

            openClSettings.RequiresTAN = UseTrueAudioNext;

            // Get list of acceptable OpenCL devices.
            OpenCLDeviceList deviceList = new(IntPtr.Zero);
            Error error = API.iplOpenCLDeviceListCreate(Context, in openClSettings, out deviceList);

            if (error != Error.Success)
            {
                printErrInfo(error.ToString());
                settings.type = fallbackType;
                SceneType = fallbackType;
                return false;
            }

            try
            {
                // Make sure we have at least one valid OpenCL device.
                int numDevices = API.iplOpenCLDeviceListGetNumDevices(deviceList.Handle);

                if (numDevices == 0)
                {
                    printErrInfo("No compatible OpenCL devices found.");
                    settings.type = fallbackType;
                    SceneType = fallbackType;
                    return false;
                }

                error = API.iplOpenCLDeviceCreate(Context, deviceList.Handle, 0, out OpenCLDevicePointer);

                if (error != Error.Success)
                {
                    printErrInfo(error.ToString());
                    settings.type = fallbackType;
                    SceneType = fallbackType;
                    return false;
                }

                return true;
            }
            finally
            {
                nint deviceListHandle = deviceList.Handle;
                API.iplOpenCLDeviceListRelease(ref deviceListHandle);
            }

            // Helper function for printing error info.
            void printErrInfo(string message)
            {
                GD.PrintErr("Failed to open OpenCL Device: " + message);
                GD.PrintErr("Continuing without OpenCL support...");
            }
        }

        /// <summary>
        /// If in the editor, this simply commits the scene immediately. If in-game, it queues a commit to be run 
        /// on the next physics process.
        /// </summary>
        internal void QueueSceneCommit()
        {
            if (Engine.IsEditorHint())
            {
                API.iplSceneCommit(Scene);
            }
            else
            {
                SceneCommitQueued = true;
            }
        }

        public unsafe void FMODEventCreated(Variant var)
        {
            CallDeferred(nameof(FmodEventCreatedTask), var);
        }

        public void FMODEventRemoved(Variant var)
        {
            CallDeferred(nameof(FmodEventDeletedTask), var);
        }

        public void FmodEventCreatedTask(Variant var)
        {
            if (Simulator == IntPtr.Zero)
            {
                GD.PrintErr("Steam Audio Simulator is not initialized! Event called too early?");
                return;
            }

           
            byte[] ptrBytes = (byte[])var;

            nint ptr = bytesToPtr(ptrBytes);

            if (ptr == IntPtr.Zero)
            {
                GD.PrintErr("FMOD EventInstance pointer is null... somehow.");
                return;
            }

            EventInstance eventInstance = new(ptr);

            DSP? steamDSP = FmodGetSteamDSP(eventInstance);

            if (steamDSP == null)
                return;

            lock(SteamAudioSimulationLock) lock (SteamAudioCollectionsLock)
            {
                // Do stuff with our Steam Audio Spatializer DSP if this source has one.
                SteamAudioSource? source = new(this, eventInstance);

                if (source == null)
                    return;

                if (source.FmodSourceHandle == null)
                    return;

                if (!CheckFmodErr(steamDSP.Value.setParameterInt((int)IPLSpatializerParams.IPL_SPATIALIZE_SIMULATION_OUTPUTS_HANDLE,
                    source.FmodSourceHandle.Value)))
                    return;

                ActiveSources.Add(source);
            }
        }

        public void FmodEventDeletedTask(Variant var)
        {
            byte[] ptrBytes = (byte[])var;

            nint ptr = bytesToPtr(ptrBytes);

            foreach (SteamAudioSource source in ActiveSources)
            {
                if (source.FmodSourceHandle == null || source.FmodEvent == null)
                    continue;

                nint sourcePtr = source.FmodEvent.Value.handle;

                if (sourcePtr == ptr)
                {
                    API.iplSourceRemove(source.Ptr, Simulator);
                    SteamFmodApi.iplFMODRemoveSource(source.FmodSourceHandle.Value);
                    lock (SteamAudioCollectionsLock)
                    {
                        ActiveSources.Remove(source);
                    }

                    break;
                }
            }
        }

        internal static DSP? FmodGetSteamDSP(EventInstance eventInstance)
        {
            RESULT err = eventInstance.getChannelGroup(out ChannelGroup channelGroup);

            if (!CheckFmodErr(err, RESULT.ERR_INVALID_HANDLE))
                return null;

            channelGroup.getNumDSPs(out int numDsps);

            for (int i = 0; i < numDsps; i++)
            {
                if (!CheckFmodErr(channelGroup.getDSP(i, out DSP dsp)))
                    continue;

                dsp.getInfo(out string name, out uint ver, out int ch, out int configwidth, out int configheight);

                if (name.Contains("Steam Audio Spatializer"))
                {
                    return dsp;
                }
            }

            return null;
        }

        public unsafe void UpdateSourceOutputs(SteamAudioSource? source, IntPtr simulator)
        {
            if (source == null)
                return;

            SimulationOutputs outputs = new();
            API.iplSourceGetOutputs(source.Ptr, SimFlags, ref outputs);

            outputs.pathing.eqCoeffsLow = Mathf.Max(0.1f, outputs.pathing.eqCoeffsLow);
            outputs.pathing.eqCoeffsMid = Mathf.Max(0.1f, outputs.pathing.eqCoeffsMid);
            outputs.pathing.eqCoeffsHigh = Mathf.Max(0.1f, outputs.pathing.eqCoeffsHigh);
        }

        internal static void IplCoordsFromFmod(ATTRIBUTES_3D attribs, ref CoordinateSpace3 coords)
        {
            Vector3 fmodUp = new(attribs.up.x, attribs.up.y, attribs.up.z);
            Vector3 fmodForward = new(attribs.forward.x, attribs.forward.y, attribs.forward.z);
            Vector3 right = fmodForward.Cross(fmodUp).Normalized();

            coords.ahead = IplVector(fmodForward);
            coords.right = IplVector(right);
            coords.up = IplVector(fmodUp);
            coords.origin = IplVector(new(attribs.position.x, attribs.position.y, attribs.position.z));
        }

        /// <summary>
        /// Shorthand method for checking FMOD errors. Returns a simple true or false, and in the case of an error
        /// prints to the console.
        /// </summary>
        /// <param name="error"></param>
        /// <param name="errorToSuppress">If it's this error, don't print anything; just silently fail.</param>
        /// <returns>true = no error, false = error</returns>
        internal static bool CheckFmodErr(RESULT error, RESULT? errorToSuppress = null)
        {
            if (error != RESULT.OK)
            {
                if (error != errorToSuppress)
                    GD.PrintErr("FMOD Error in FmodSteamAudioBridge: " + error);

                return false;
            }

            return true;
        }

        private unsafe nint bytesToPtr(byte[] bytes)
        {
            byte* ptrFixed = stackalloc byte[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                ptrFixed[i] = bytes[i];
            }

            nint* ptr = (nint*)ptrFixed;

            return *ptr;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (Engine.IsEditorHint())
                return;

            if (ListenerNode == null
                || Simulator == IntPtr.Zero)
            {
                GD.PrintErr("ListenerNode or Simulator not set in FmodSteamAudioBridge!");
                return;
            }

            if (ReverbListenerSource != null && ReverbListenerSource.IsValid)
            {
                ReverbListenerSource.SourceTransform = ListenerNode.GlobalTransform;
            }

            if (SceneCommitQueued)
            {
                lock (SteamAudioSimulationLock)
                {
                    API.iplSceneCommit(Scene);
                    API.iplSimulatorCommit(Simulator);
                    SceneCommitQueued = false;
                }
            }

            Transform3D transform = ListenerNode.GlobalTransform;

            SharedInputs = new()
            {
                numRays = MaxReflectionRays,
                numBounces = 4,
                duration = 1.0f,
                order = 2,
                irradianceMinDistance = 1.0f,
                pathingUserData = IntPtr.Zero
            };

            SharedInputs.listener.ahead = IplVector(transform * Vector3.Forward);
            SharedInputs.listener.right = IplVector(transform * Vector3.Right);
            SharedInputs.listener.up = IplVector(transform * Vector3.Up);
            SharedInputs.listener.origin = IplVector(transform.Origin);

            lock (SteamAudioCollectionsLock)
            {
                foreach (SteamAudioSource source in ActiveSources)
                {
                    if (source.FmodGuid == string.Empty)
                        continue;

                    if (OcclusionProcessors.TryGetValue(source.FmodGuid, out OcclusionProcessor? processor))
                    {
                        float occlusionValue = processor.ProcessOcclusion(GetWorld3D(), source, ListenerNode);

                        source.UpdateCustomOcclusion(occlusionValue);
                    }
                }
            }
        }

        public void RunSimulation()
        {
            while (simulationThreadRunning)
            {
                Thread.Sleep(1);

                if (!simulationThreadRunning)
                    break;

                if (Context == IntPtr.Zero || Simulator == IntPtr.Zero)
                    continue;

                // Run direct
                API.iplSimulatorSetSharedInputs(Simulator, SimulationFlags.Direct, ref SharedInputs);

                lock(SteamAudioCollectionsLock)
                {
                    foreach (SteamAudioSource source in ActiveSources)
                    {
                        source.UpdateSourceInputs(this, Simulator, SimulationFlags.Direct);
                    }
                }

                lock (SteamAudioSimulationLock)
                {
                    API.iplSimulatorRunDirect(Simulator);
                }
                    
                // Run reflections
                API.iplSimulatorSetSharedInputs(Simulator, SimulationFlags.Reflections, ref SharedInputs);

                lock (SteamAudioCollectionsLock)
                {
                    foreach (SteamAudioSource source in ActiveSources)
                    {
                        source.UpdateSourceInputs(this, Simulator, SimulationFlags.Reflections);
                    }
                }
                
                if (ReverbListenerSource != null && ReverbListenerSource.IsValid)
                {
                    ReverbListenerSource.UpdateSourceInputs(this, Simulator, SimulationFlags.Reflections);
                }

                lock (SteamAudioSimulationLock)
                {
                    API.iplSimulatorRunReflections(Simulator);
                }

                // Run pathing
                if (SimulatePathing)
                {
                    API.iplSimulatorSetSharedInputs(Simulator, SimulationFlags.Pathing, ref SharedInputs);

                    lock (SteamAudioCollectionsLock)
                    {
                        foreach (SteamAudioSource source in ActiveSources)
                        {
                            source.UpdateSourceInputs(this, Simulator, SimulationFlags.Pathing);
                        }
                    }

                    lock (SteamAudioSimulationLock)
                    {
                        API.iplSimulatorRunPathing(Simulator);
                    }
                }
            }
        }

        /// <summary>
        /// GUID should be in the format "{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}", including the curly braces.
        /// </summary>
        /// <param name="fmodEventGuid"></param>
        /// <param name="processor"></param>
        public void RegisterOcclusionProcessor(string fmodEventGuid, OcclusionProcessor processor)
        {
            OcclusionProcessors[fmodEventGuid] = processor;
        }

        internal static SteamAudio.Vector3 IplVector(Vector3 v)
        {
            return new SteamAudio.Vector3()
            {
                x = v.X,
                y = v.Y,
                z = v.Z
            };
        }
    }
}

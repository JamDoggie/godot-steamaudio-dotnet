using Godot;
using Godot.Collections;
using SteamAudioDotnet.scripts.nativelib;
using System;
using SteamAudio;
using Error = SteamAudio.Error;
using System.Collections.Generic;
using SteamAudioDotnet.scripts.steamaudio.resource;
using System.Net;

namespace SteamAudioDotnet.scripts.steamaudio.nodes
{
    [GlobalClass, Tool, Icon("res://addons/steam-audio-cs-plugin/materials/icons/hammer-sound-icon.png")]
    public partial class SteamAudioStaticGeometry : Node3D
    {
        public List<IntPtr> SteamAudioMeshPtrs = new();
        private FmodSteamAudioBridge? steamAudioInstance = null;

        [Export]
        public bool AutoInitialize = true;
        [Export]
        public Array<NodePath> MeshInstances = [];

        private List<MeshInstance3D> childrenMeshes = new();

        public override string[] _GetConfigurationWarnings()
        {
            Array<Node> children = GetChildren();

            bool hasCollisionShape = false;
            bool hasMultipleShapes = false;

            foreach (Node child in children)
            {
                if (child is MeshInstance3D)
                {
                    if (hasCollisionShape)
                    {
                        hasMultipleShapes = true;
                        break;
                    }

                    hasCollisionShape = true;
                }
            }

            if (!hasCollisionShape)
            {
                return
                [
                    $"{GetType().Name} node has no {nameof(MeshInstance3D)} children. " +
                    "Static geometry requires a collision shape to function properly." 
                ];
            }
            else if (hasMultipleShapes)
            {
                return
                [
                    $"{GetType().Name} node cannot have more than one {nameof(MeshInstance3D)} children. " +
                    "Static geometry requires a single collision shape to function properly."
                ];
            }

            return [];
        }

        public override void _Ready()
        {
            if (AutoInitialize && !Engine.IsEditorHint())
            {
                SetupCollision(FmodSteamAudioBridge.Singleton);
            }
        }

        public void Cleanup()
        {
            if (SteamAudioMeshPtrs.Count > 0 &&
                steamAudioInstance != null &&
                steamAudioInstance.Scene != IntPtr.Zero)
            {
                foreach (IntPtr meshPtr in SteamAudioMeshPtrs)
                {
                    CleanupStaticMesh(meshPtr, steamAudioInstance.Scene);
                }

                SteamAudioMeshPtrs.Clear();
            }
        }

        public static void CleanupStaticMesh(IntPtr staticMeshPtr, IntPtr scene)
        {
            if (staticMeshPtr != IntPtr.Zero &&
                scene != IntPtr.Zero)
            {
                GD.Print("CLEANING UP STEAM AUDIO STATIC MESH");

                API.iplStaticMeshRemove(staticMeshPtr, scene);
                API.iplSceneCommit(scene);
            }
        }

        public override void _Process(double delta)
        {
        }

        public void SetupCollision(FmodSteamAudioBridge? steamAudio)
        {
            #region Initial error checking
            if (steamAudio == null)
            {
                GD.PrintErr($"{GetType().Name} node cannot get Steam Audio mesh because there is no " +
                    $"{nameof(FmodSteamAudioBridge)} instance in the scene.");
                return;
            }

            if (steamAudio.Context == IntPtr.Zero ||
                steamAudio.Scene == IntPtr.Zero)
            {
                GD.PrintErr($"{GetType().Name} node cannot get Steam Audio mesh because the " +
                    $"{nameof(FmodSteamAudioBridge)} instance has not been initialized.");
                GD.PrintErr($"Context: {steamAudio.Context}");
                GD.PrintErr($"Scene: {steamAudio.Scene}");
                return;
            }
            #endregion

            childrenMeshes.Clear();

            IntPtr scene = steamAudio.Scene;

            List<IntPtr> meshes = new();

            for (int i = 0; i < MeshInstances.Count; i++)
            {
                NodePath nodePath = MeshInstances[i];

                Node? node = GetNodeOrNull(nodePath);

                if (node == null)
                    continue;

                if (node is MeshInstance3D)
                    continue;

                foreach (Node childNode in node.GetChildren())
                {
                    if (childNode is MeshInstance3D meshInstance)
                    {
                        childrenMeshes.Add(meshInstance);
                    }
                }
            }

            foreach (NodePath path in MeshInstances)
            {
                if (GetNodeOrNull(path) is MeshInstance3D meshInstance)
                {
                    childrenMeshes.Add(meshInstance);
                }
            }

            foreach (MeshInstance3D meshInstance in childrenMeshes)
            {
                IntPtr meshPtr = GetSteamAudioMesh(scene, meshInstance);

                if (meshPtr == IntPtr.Zero)
                {
                    GD.PrintErr($"{GetType().Name} node failed to create Steam Audio static mesh.");
                    return;
                }

                SteamAudioMeshPtrs.Add(meshPtr);

                API.iplStaticMeshAdd(meshPtr, scene);

                steamAudio.QueueSceneCommit();

                GD.Print("Successfully added Steam Audio static mesh for " +
                    $"{GetType().Name} node to scene.");
            }
        }

        public override void _ExitTree()
        {
            Cleanup();
        }

        public unsafe IntPtr GetSteamAudioMesh(IntPtr scene, MeshInstance3D meshInstance)
        {
            IntPtr staticMesh = IntPtr.Zero;

            Mesh? mesh = meshInstance.Mesh;

            // Some of the following code was made in reference to the Godot Steam Audio GDExtension.
            // https://github.com/stechyo/godot-steam-audio/blob/master/src/geometry_common.hpp

            // Convert our mesh to something steam audio will understand.
            List<Godot.Vector3> verts = new();
            List<int> tris = new();
            List<int> materialList = new();

            int surfaceCount = mesh.GetSurfaceCount();

            for (int surfIndex = 0; surfIndex < surfaceCount; surfIndex++)
            {
                Godot.Collections.Array dat = mesh.SurfaceGetArrays(surfIndex);

                Variant vertices = dat[(int)Mesh.ArrayType.Vertex];
                Variant indices = dat[(int)Mesh.ArrayType.Index];

                if (vertices.VariantType != Variant.Type.PackedVector3Array || indices.VariantType != Variant.Type.PackedInt32Array)
                {
                    GD.PrintErr($"Failed to get vertices or indices from mesh in {GetType().Name} node.");
                    return staticMesh;
                }

                Godot.Vector3[] currentSurfaceVerts = (Godot.Vector3[])vertices;
                int[] currentSurfaceTris = (int[])indices;

                int vertOffset = verts.Count;

                verts.AddRange(currentSurfaceVerts);
                for (int i = 0; i < currentSurfaceTris.Length; i++)
                {
                    tris.Add(currentSurfaceTris[i] + vertOffset);
                }

                for (int i = 0; i < currentSurfaceTris.Length / 3; i++)
                {
                    materialList.Add(surfIndex);
                }
            }

            SteamAudio.Vector3* ipl_verts = stackalloc SteamAudio.Vector3[verts.Count];
            Triangle* ipl_tris = stackalloc Triangle[tris.Count / 3];
            int* ipl_mat_indices = stackalloc int[tris.Count / 3];

            Transform3D transform = meshInstance.GetGlobalTransform();

            // Transform mesh vertices by the collision shape's transform
            for (int i = 0; i < verts.Count; i++)
            {
                Godot.Vector3 vert = verts[i];

                vert = transform * vert;
                ipl_verts[i] = new SteamAudio.Vector3() { x = vert.X, y = vert.Y, z = vert.Z };
            }

            // Convert from clockwise to counter-clockwise winding order
            for (int i = 0; i < tris.Count; i += 3)
            {
                ipl_tris[i / 3].index0 = tris[i];
                ipl_tris[i / 3].index1 = tris[i + 2];
                ipl_tris[i / 3].index2 = tris[i + 1];
                ipl_mat_indices[i / 3] = materialList[i / 3];
            }

            SteamAudio.Material* materialArray = stackalloc SteamAudio.Material[surfaceCount];

            // If the material resource has no SteamAudioMaterialScript attached, use a fallback material.
            // This is just the plaster material from the Steam Audio docs.
            SteamAudio.Material fallbackMaterial = new();

            float[] defaultMat = { 0.12f, 0.06f, 0.04f, 0.05f, 0.056f, 0.056f, 0.004f}; // Plaster

            fallbackMaterial.absorptionLow = defaultMat[0];
            fallbackMaterial.absorptionMid = defaultMat[1];
            fallbackMaterial.absorptionHigh = defaultMat[2];
            fallbackMaterial.scattering = defaultMat[3];
            fallbackMaterial.transmissionLow = defaultMat[4];
            fallbackMaterial.transmissionMid = defaultMat[5];
            fallbackMaterial.transmissionHigh = defaultMat[6];

            for (int i = 0; i < surfaceCount; i++)
            {
                Godot.Material? steamAudioMatScript = mesh.SurfaceGetMaterial(i);

                if (meshInstance.GetSurfaceOverrideMaterial(i) is Godot.Material overrideMat)
                {
                    steamAudioMatScript = overrideMat;
                }

                if (steamAudioMatScript == null)
                {
                    GD.PrintErr($"Material {i} on mesh in {GetType().Name} node was null. Using fallback material.");
                    materialArray[i] = fallbackMaterial;
                    continue;
                }

                if (steamAudioMatScript.Get("SteamAudioMaterial").Obj == null)
                {
                    GD.PrintErr($"Material {i} on mesh in {GetType().Name} node has no Steam Audio material resource. Using fallback material.");
                    materialArray[i] = fallbackMaterial;
                    continue;
                }

                Variant var = steamAudioMatScript.Get("SteamAudioMaterial");

                if (var.Obj is not SteamAudioMaterialResource steamAudioMat)
                {
                    GD.PrintErr($"Material {i} on mesh in {GetType().Name} node has invalid Steam Audio material resource. Using fallback material.");
                    materialArray[i] = fallbackMaterial;
                    continue;
                }

                SteamAudio.Material currentMaterial = new()
                {
                    absorptionLow = steamAudioMat.AbsorptionLowFreq,
                    absorptionMid = steamAudioMat.AbsorptionMidFreq,
                    absorptionHigh = steamAudioMat.AbsorptionHighFreq,
                    scattering = steamAudioMat.Scattering,
                    transmissionLow = steamAudioMat.TransmissionLowFreq,
                    transmissionMid = steamAudioMat.TransmissionMidFreq,
                    transmissionHigh = steamAudioMat.TransmissionHighFreq
                };

                materialArray[i] = currentMaterial;
            }

            StaticMeshSettings staticMeshSettings = new()
            {
                numVertices = verts.Count,
                numTriangles = tris.Count / 3,
                numMaterials = surfaceCount,
                vertices = (IntPtr)ipl_verts,
                triangles = (IntPtr)ipl_tris,
                materialIndices = (IntPtr)ipl_mat_indices,
                materials = (IntPtr)materialArray
            };

            Error error = API.iplStaticMeshCreate(scene, ref staticMeshSettings, out staticMesh);

            if (error != Error.Success)
            {

                GD.PrintErr($"Failed to create Steam Audio static mesh in {GetType().Name} node. " +
                    $"Error code: {error}");
                return IntPtr.Zero;
            }

            return staticMesh;
        }
    }
}

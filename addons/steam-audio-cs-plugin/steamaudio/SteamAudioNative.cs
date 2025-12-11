//
// Copyright 2017-2023 Valve Corporation.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

// https://github.com/ValveSoftware/steam-audio

using Godot;
using System;
using System.Runtime.InteropServices;

namespace SteamAudio
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public readonly partial struct OpenCLDeviceList : IEquatable<OpenCLDeviceList>
    {
        private readonly nint _handle;

        public OpenCLDeviceList(nint handle) => _handle = handle;

        public nint Handle => _handle;

        public bool Equals(OpenCLDeviceList other) => _handle.Equals(other._handle);

        public override bool Equals(object obj) => obj is OpenCLDeviceList other && Equals(other);

        public override int GetHashCode() => _handle.GetHashCode();

        public override string ToString() => "0x" + (nint.Size == 8 ? _handle.ToString("X16") : _handle.ToString("X8"));

        public static bool operator ==(OpenCLDeviceList left, OpenCLDeviceList right) => left.Equals(right);

        public static bool operator !=(OpenCLDeviceList left, OpenCLDeviceList right) => !left.Equals(right);
    }

    // CONSTANTS

    public static class Constants
    {
        public const uint kVersionMajor = 4;
        public const uint kVersionMinor = 7;
        public const uint kVersionPatch = 0;
        public const uint kVersion = kVersionMajor << 16 | kVersionMinor << 8 | kVersionPatch;
    }

    // ENUMERATIONS

    public enum Bool
    {
        False,
        True
    }

    public enum Error
    {
        Success,
        Failure,
        OutOfMemory,
        Initialization
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }

    public enum SIMDLevel
    {
        SSE2,
        SSE4,
        AVX,
        AVX2,
        AVX512,
        NEON = SSE2
    }

    [Flags]
    public enum ContextFlags
    {
        Validation = 1 << 0,
        Force32Bit = 0x7fffffff
    }

    public enum OpenCLDeviceType : int
    {
        Any,
        CPU,
        GPU
    }

    public enum SceneType
    {
        Default,
        Embree,
        RadeonRays,
        Custom
    }

    public enum HRTFType
    {
        Default,
        SOFA
    }

    public enum HRTFNormType
    {
        None,
        RMS
    }

    public enum ProbeGenerationType
    {
        Centroid,
        UniformFloor
    }

    public enum BakedDataVariation
    {
        Reverb,
        StaticSource,
        StaticListener,
        Dynamic
    }

    public enum BakedDataType
    {
        Reflections,
        Pathing
    }

    [Flags]
    public enum SimulationFlags
    {
        Direct = 1 << 0,
        Reflections = 1 << 1,
        Pathing = 1 << 2
    }

    [Flags]
    public enum DirectSimulationFlags
    {
        DistanceAttenuation = 1 << 0,
        AirAbsorption = 1 << 1,
        Directivity = 1 << 2,
        Occlusion = 1 << 3,
        Transmission = 1 << 4
    }

    public enum HRTFInterpolation
    {
        Nearest,
        Bilinear
    }

    public enum DistanceAttenuationModelType
    {
        Default,
        InverseDistance,
        Callback
    }

    public enum AirAbsorptionModelType
    {
        Default,
        Exponential,
        Callback
    }

    public enum DeviationModelType
    {
        Default,
        Callback
    }

    public enum OcclusionType
    {
        Raycast,
        Volumetric
    }

    [Flags]
    public enum DirectEffectFlags
    {
        ApplyDistanceAttenuation = 1 << 0,
        ApplyAirAbsorption = 1 << 1,
        ApplyDirectivity = 1 << 2,
        ApplyOcclusion = 1 << 3,
        ApplyTransmission = 1 << 4
    }

    public enum TransmissionType
    {
        FrequencyIndependent,
        FrequencyDependent
    }

    public enum ReflectionEffectType
    {
        Convolution,
        Parametric,
        Hybrid,
#if UNITY_2019_2_OR_NEWER
        [InspectorName("TrueAudio Next")]
#endif
        TrueAudioNext
    }

    /// <summary>
    /// Flags for specifying what types of reflections data to bake.
    /// </summary>
    [Flags]
    public enum ReflectionsBakeFlags : int
    {
        /// <summary>
        /// Bake impulse responses for @c IPL_REFLECTIONEFFECTTYPE_CONVOLUTION, @c IPL_REFLECTIONEFFECTTYPE_HYBRID, or @c IPL_REFLECTIONEFFECTTYPE_TAN.
        /// </summary>
        BakeConvolution = unchecked((int)1 << (int)0),

        /// <summary>
        /// Bake parametric reverb for @c IPL_REFLECTIONEFFECTTYPE_PARAMETRIC or @c IPL_REFLECTIONEFFECTTYPE_HYBRID.
        /// </summary>
        BakeParametric = unchecked((int)1 << (int)1),
    }

    // CALLBACKS

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate void ProgressCallback(float progress, nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate void LogCallback(LogLevel level, string message);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate nint AllocateCallback(nuint size, nuint alignment);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate void FreeCallback(nint memoryBlock);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate void ClosestHitCallback(ref Ray ray, float minDistance, float maxDistance, out Hit hit, nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate void AnyHitCallback(ref Ray ray, float minDistance, float maxDistance, out byte occluded, nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate void BatchedClosestHitCallback(int numRays, Ray[] rays, float[] minDistances, float[] maxDistances, [Out] Hit[] hits, nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate void BatchedAnyHitCallback(int numRays, Ray[] rays, float[] minDistances, float[] maxDistances, [Out] byte[] occluded, nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate float DistanceAttenuationCallback(float distance, nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate float AirAbsorptionCallback(float distance, int band, nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate float DeviationCallback(float angle, int band, nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate float DirectivityCallback(Vector3 direction, nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate void PathingVisualizationCallback(Vector3 from, Vector3 to, Bool occluded, nint userData);

    // STRUCTURES

    [StructLayout(LayoutKind.Sequential)]
    public struct ContextSettings
    {
        public uint version;
        public LogCallback logCallback;
        public AllocateCallback allocateCallback;
        public FreeCallback freeCallback;
        public SIMDLevel simdLevel;
        public ContextFlags flags;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix4x4
    {
        public float m00;
        public float m01;
        public float m02;
        public float m03;
        public float m10;
        public float m11;
        public float m12;
        public float m13;
        public float m20;
        public float m21;
        public float m22;
        public float m23;
        public float m30;
        public float m31;
        public float m32;
        public float m33;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Box
    {
        public Vector3 minCoordinates;
        public Vector3 maxCoordinates;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Sphere
    {
        public Vector3 center;
        public float radius;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CoordinateSpace3
    {
        public Vector3 right;
        public Vector3 up;
        public Vector3 ahead;
        public Vector3 origin;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SerializedObjectSettings
    {
        public nint data;
        public nuint size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EmbreeDeviceSettings { }

    /// <summary>
    /// Specifies requirements that an OpenCL device must meet in order to be considered when listing
    /// OpenCL devices.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public partial struct OpenCLDeviceSettings
    {
        /// <summary>
        /// The type of device. Set to @c IPL_OPENCLDEVICETYPE_ANY to consider all available devices.
        /// </summary>
        public OpenCLDeviceType Type;

        /// <summary>
        /// The number of GPU compute units (CUs) that should be reserved for use by Steam Audio. If set to a
        /// non-zero value, then a GPU will be included in the device list only if it can reserve at least
        /// this many CUs. Set to 0 to indicate that Steam Audio can use the entire GPU, in which case all
        /// available GPUs will be considered.
        /// </summary>
        /// <remarks>
        /// Ignored if @c type is @c IPL_OPENCLDEVICETYPE_CPU.
        /// </remarks>
        public int NumCUsToReserve;

        /// <summary>
        /// The fraction of reserved CUs that should be used for impulse response (IR) update. IR update
        /// includes: a) ray tracing using Radeon Rays to simulate sound propagation, and/or b) pre-transformation
        /// of IRs for convolution using TrueAudio Next. Steam Audio will only list GPU devices that are able
        /// to subdivide the reserved CUs as per this value. The value must be between 0 and 1.
        /// </summary>
        /// <remarks>
        /// For example, if @c numCUsToReserve is @c 8, and @c fractionCUsForIRUpdate is @c 0.5f, then 4 CUs
        /// will be used for IR update and 4 CUs will be used for convolution. Below are typical scenarios:-   Using only TrueAudio Next. Set @c fractionCUsForIRUpdate to @c 0.5f. This ensures that reserved
        /// CUs are available for IR update as well as convolution.-   Using TrueAudio Next and Radeon Rays for real-time simulation and rendering. Choosing
        /// @c fractionCUsForIRUpdate may require some experimentation to utilize reserved CUs optimally. You
        /// can start by setting @c fractionCUsForIRUpdate to @c 0.5f. However, if IR calculation has high
        /// latency with these settings, increase @c fractionCUsForIRUpdate to use more CUs for ray tracing.-   Using only Radeon Rays. Set @c fractionCUsForIRUpdate to @c 1, to make sure all the reserved CUs
        /// are used for ray tracing. If using Steam Audio for preprocessing (e.g. baking reverb), then
        /// consider setting @c numCUsToReserve to @c 0 to use the entire GPU for accelerated ray tracing.Ignored if @c type is @c IPL_OPENCLDEVICETYPE_CPU or @c numCUsToReserve is @c 0.
        /// </remarks>
        public float FractionCUsForIRUpdate;

        /// <summary>
        /// If @c IPL_TRUE, then the GPU device must support TrueAudio Next. It is not necessary to set this
        /// to @c IPL_TRUE if @c numCUsToReserve or @c fractionCUsForIRUpdate are set to non-zero values.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool RequiresTAN;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OpenCLDeviceDesc
    {
        public nint platform;
        public string platformName;
        public string platformVendor;
        public string platformVersion;
        public nint device;
        public string deviceName;
        public string deviceVendor;
        public string deviceVersion;
        public OpenCLDeviceType type;
        public int numConvolutionCUs;
        public int numIRUpdateCUs;
        public int granularity;
        public float perfScore;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RadeonRaysDeviceSettings { }

    [StructLayout(LayoutKind.Sequential)]
    public struct TrueAudioNextDeviceSettings
    {
        public int frameSize;
        public int irSize;
        public int order;
        public int maxSources;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Triangle
    {
        public int index0;
        public int index1;
        public int index2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Material
    {
        public float absorptionLow;
        public float absorptionMid;
        public float absorptionHigh;
        public float scattering;
        public float transmissionLow;
        public float transmissionMid;
        public float transmissionHigh;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Ray
    {
        public Vector3 origin;
        public Vector3 direction;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Hit
    {
        public float distance;
        public int triangleIndex;
        public int objectIndex;
        public int materialIndex;
        public Vector3 normal;
        public nint material;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SceneSettings
    {
        public SceneType type;
        public ClosestHitCallback closestHitCallback;
        public AnyHitCallback anyHitCallback;
        public BatchedClosestHitCallback batchedClosestHitCallback;
        public BatchedAnyHitCallback batchedAnyHitCallback;
        public nint userData;
        public nint embreeDevice;
        public nint radeonRaysDevice;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StaticMeshSettings
    {
        public int numVertices;
        public int numTriangles;
        public int numMaterials;
        public nint vertices;
        public nint triangles;
        public nint materialIndices;
        public nint materials;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InstancedMeshSettings
    {
        public nint subScene;
        public Matrix4x4 transform;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AudioSettings
    {
        public int samplingRate;
        public int frameSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HRTFSettings
    {
        public HRTFType type;
        public string sofaFileName;
        public nint sofaFileData;
        public int sofaFileDataSize;
        public float volume;
        public HRTFNormType normType;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ProbeGenerationParams
    {
        public ProbeGenerationType type;
        public float spacing;
        public float height;
        public Matrix4x4 transform;
    }

    /// <summary>
    /// Identifies a "layer" of data stored in a probe batch. Each probe batch may store multiple layers of data,
    /// such as reverb, static source reflections, or pathing. Each layer can be accessed using an identifier.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public partial struct BakedDataIdentifier
    {
        /// <summary>
        /// The type of data stored.
        /// </summary>
        public BakedDataType type;

        /// <summary>
        /// The way in which source and listener positions depend on probe position.
        /// </summary>
        public BakedDataVariation variation;

        /// <summary>
        /// The static source (for @c IPL_BAKEDDATAVARIATION_STATICSOURCE) or static listener (for
        /// @c IPL_BAKEDDATAVARIATION_STATICLISTENER) used to generate baked data. Baked data is only stored for
        /// probes that lie within the radius of this sphere.
        /// </summary>
        public Sphere endpointInfluence;
    }

    /// <summary>
    /// Parameters used to control how reflections data is baked.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public partial struct ReflectionsBakeParams
    {
        /// <summary>
        /// The scene in which the probes exist.
        /// </summary>
        public nint scene;

        /// <summary>
        /// A probe batch containing the probes at which reflections data should be baked.
        /// </summary>
        public nint probeBatch;

        /// <summary>
        /// The type of scene being used.
        /// </summary>
        public SceneType sceneType;

        /// <summary>
        /// An identifier for the data layer that should be baked. The identifier determines what data is simulated and
        /// stored at each probe. If the probe batch already contains data with this identifier, it will be overwritten.
        /// </summary>
        public BakedDataIdentifier identifier;

        /// <summary>
        /// The types of data to save for each probe.
        /// </summary>
        public ReflectionsBakeFlags bakeFlags;

        /// <summary>
        /// The number of rays to trace from each listener position when baking. Increasing this number results in
        /// improved accuracy, at the cost of increased bake times.
        /// </summary>
        public int numRays;

        /// <summary>
        /// The number of directions to consider when generating diffusely-reflected rays when baking. Increasing
        /// this number results in slightly improved accuracy of diffuse reflections.
        /// </summary>
        public int numDiffuseSamples;

        /// <summary>
        /// The number of times each ray is reflected off of solid geometry. Increasing this number results in
        /// longer reverb tails and improved accuracy, at the cost of increased bake times.
        /// </summary>
        public int numBounces;

        /// <summary>
        /// The length (in seconds) of the impulse responses to simulate. Increasing this number allows the baked
        /// data to represent longer reverb tails (and hence larger spaces), at the cost of increased memory
        /// usage while baking.
        /// </summary>
        public float simulatedDuration;

        /// <summary>
        /// The length (in seconds) of the impulse responses to save at each probe. Increasing this number allows
        /// the baked data to represent longer reverb tails (and hence larger spaces), at the cost of increased
        /// disk space usage and memory usage at run-time.
        /// </summary>
        /// <remarks>
        /// It may be useful to set @c savedDuration to be less than @c simulatedDuration, especially if you plan
        /// to use hybrid reverb for rendering baked reflections. This way, the parametric reverb data is
        /// estimated using a longer IR, resulting in more accurate estimation, but only the early part of the IR
        /// can be saved for subsequent rendering.
        /// </remarks>
        public float savedDuration;

        /// <summary>
        /// Ambisonic order of the baked IRs.
        /// </summary>
        public int order;

        /// <summary>
        /// Number of threads to use for baking.
        /// </summary>
        public int numThreads;

        /// <summary>
        /// If using custom ray tracer callbacks, this the number of rays that will be passed to the callbacks
        /// every time rays need to be traced.
        /// </summary>
        public int rayBatchSize;

        /// <summary>
        /// When calculating how much sound energy reaches a surface directly from a source, any source that is
        /// closer than @c irradianceMinDistance to the surface is assumed to be at a distance of
        /// @c irradianceMinDistance, for the purposes of energy calculations.
        /// </summary>
        public float irradianceMinDistance;

        /// <summary>
        /// If using Radeon Rays or if @c identifier.variation is @c IPL_BAKEDDATAVARIATION_STATICLISTENER, this is the
        /// number of probes for which data is baked simultaneously.
        /// </summary>
        public int bakeBatchSize;

        /// <summary>
        /// The OpenCL device, if using Radeon Rays.
        /// </summary>
        public nint openCLDevice;

        /// <summary>
        /// The Radeon Rays device, if using Radeon Rays.
        /// </summary>
        public nint radeonRaysDevice;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PathBakeParams
    {
        public nint scene;
        public nint probeBatch;
        public BakedDataIdentifier identifier;
        public int numSamples;
        public float radius;
        public float threshold;
        public float visRange;
        public float pathRange;
        public int numThreads;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DistanceAttenuationModel
    {
        public DistanceAttenuationModelType type;
        public float minDistance;
        public DistanceAttenuationCallback callback;
        public nint userData;
        public Bool dirty;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AirAbsorptionModel
    {
        public AirAbsorptionModelType type;
        public float coefficientsLow;
        public float coefficientsMid;
        public float coefficientsHigh;
        public AirAbsorptionCallback callback;
        public nint userData;
        public Bool dirty;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Directivity
    {
        public float dipoleWeight;
        public float dipolePower;
        public DirectivityCallback callback;
        public nint userData;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DeviationModel
    {
        public DeviationModelType type;
        public nint callback;
        public nint userData;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SimulationSettings
    {
        public SimulationFlags flags;
        public SceneType sceneType;
        public ReflectionEffectType reflectionType;
        public int maxNumOcclusionSamples;
        public int maxNumRays;
        public int numDiffuseSamples;
        public float maxDuration;
        public int maxOrder;
        public int maxNumSources;
        public int numThreads;
        public int rayBatchSize;
        public int numVisSamples;
        public int samplingRate;
        public int frameSize;
        public nint openCLDevice;
        public nint radeonRaysDevice;
        public nint tanDevice;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SourceSettings
    {
        public SimulationFlags flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SimulationInputs
    {
        public SimulationFlags flags;
        public DirectSimulationFlags directFlags;
        public CoordinateSpace3 source;
        public DistanceAttenuationModel distanceAttenuationModel;
        public AirAbsorptionModel airAbsorptionModel;
        public Directivity directivity;
        public OcclusionType occlusionType;
        public float occlusionRadius;
        public int numOcclusionSamples;
        public float reverbScaleLow;
        public float reverbScaleMid;
        public float reverbScaleHigh;
        public float hybridReverbTransitionTime;
        public float hybridReverbOverlapPercent;
        public Bool baked;
        public BakedDataIdentifier bakedDataIdentifier;
        public nint pathingProbes;
        public float visRadius;
        public float visThreshold;
        public float visRange;
        public int pathingOrder;
        public Bool enableValidation;
        public Bool findAlternatePaths;
        public int numTransmissionRays;
        public nint deviationModel;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SimulationSharedInputs
    {
        public CoordinateSpace3 listener;
        public int numRays;
        public int numBounces;
        public float duration;
        public int order;
        public float irradianceMinDistance;
        public PathingVisualizationCallback pathingVisualizationCallback;
        public nint pathingUserData;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DirectEffectParams
    {
        public DirectEffectFlags flags;
        public TransmissionType transmissionType;
        public float distanceAttenuation;
        public float airAbsorptionLow;
        public float airAbsorptionMid;
        public float airAbsorptionHigh;
        public float directivity;
        public float occlusion;
        public float transmissionLow;
        public float transmissionMid;
        public float transmissionHigh;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ReflectionEffectParams
    {
        public ReflectionEffectType type;
        public nint ir;
        public float reverbTimesLow;
        public float reverbTimesMid;
        public float reverbTimesHigh;
        public float eqLow;
        public float eqMid;
        public float eqHigh;
        public int delay;
        public int numChannels;
        public int irSize;
        public nint tanDevice;
        public int tanSlot;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PathEffectParams
    {
        public float eqCoeffsLow;
        public float eqCoeffsMid;
        public float eqCoeffsHigh;
        public nint shCoeffs;
        public int order;
        public Bool binaural;
        public nint hrtf;
        public CoordinateSpace3 listener;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SimulationOutputs
    {
        public DirectEffectParams direct;
        public ReflectionEffectParams reflections;
        public PathEffectParams pathing;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PerspectiveCorrection
    {
        public Bool enabled;
        public float xfactor;
        public float yfactor;
        public Matrix4x4 transform;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EnergyFieldSettings
    {
        public float duration;
        public int order;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImpulseResponseSettings
    {
        public float duration;
        public int order;
        public int samplingRate;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ReconstructorSettings
    {
        public float maxDuration;
        public int maxOrder;
        public int samplingRate;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ReconstructorInputs
    {
        public nint energyField;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ReconstructorSharedInputs
    {
        public float duration;
        public int order;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ReconstructorOutputs
    {
        public nint impulseResponse;
    }

    // FUNCTIONS

    public static class API
    {
        public const string Library = "phonon.dll";

        // Context

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplContextCreate(ref ContextSettings settings, out nint context);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplContextRetain(nint context);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplContextRelease(ref nint context);

        // Geometry

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Vector3 iplCalculateRelativeDirection(nint context, Vector3 sourcePosition, Vector3 listenerPosition, Vector3 listenerAhead, Vector3 listenerUp);

        // Serialization

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplSerializedObjectCreate(nint context, ref SerializedObjectSettings settings, out nint serializedObject);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplSerializedObjectRetain(nint serializedObject);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSerializedObjectRelease(ref nint serializedObject);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nuint iplSerializedObjectGetSize(nint serializedObject);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplSerializedObjectGetData(nint serializedObject);

        // Embree

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplEmbreeDeviceCreate(nint context, ref EmbreeDeviceSettings settings, out nint device);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplEmbreeDeviceRetain(nint device);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplEmbreeDeviceRelease(ref nint device);

        // OpenCL
        [DllImport(Library, EntryPoint = "iplOpenCLDeviceListCreate", CallingConvention=CallingConvention.Cdecl)]
        public static extern Error iplOpenCLDeviceListCreate(nint context, in OpenCLDeviceSettings settings, out OpenCLDeviceList deviceList);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplOpenCLDeviceListRetain(nint deviceList);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplOpenCLDeviceListRelease(ref nint deviceList);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern int iplOpenCLDeviceListGetNumDevices(nint deviceList);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplOpenCLDeviceListGetDeviceDesc(nint deviceList, int index, out OpenCLDeviceDesc deviceDesc);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplOpenCLDeviceCreate(nint context, nint deviceList, int index, out nint device);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplOpenCLDeviceRetain(nint device);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplOpenCLDeviceRelease(ref nint device);

        // Radeon Rays

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplRadeonRaysDeviceCreate(nint openCLDevice, ref RadeonRaysDeviceSettings settings, out nint rrDevice);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplRadeonRaysDeviceRetain(nint device);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplRadeonRaysDeviceRelease(ref nint device);

        // TrueAudio Next

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplTrueAudioNextDeviceCreate(nint openCLDevice, ref TrueAudioNextDeviceSettings settings, out nint tanDevice);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplTrueAudioNextDeviceRetain(nint device);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplTrueAudioNextDeviceRelease(ref nint device);

        // Scene

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplSceneCreate(nint context, ref SceneSettings settings, out nint scene);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplSceneRetain(nint scene);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSceneRelease(ref nint scene);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplSceneLoad(nint context, ref SceneSettings settings, nint serializedObject, ProgressCallback progressCallback, nint progressCallbackUserData, out nint scene);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSceneSave(nint scene, nint serializedObject);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSceneSaveOBJ(nint scene, string fileBaseName);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSceneCommit(nint scene);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplStaticMeshCreate(nint scene, ref StaticMeshSettings settings, out nint staticMesh);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplStaticMeshRetain(nint staticMesh);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplStaticMeshRelease(ref nint staticMesh);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplStaticMeshLoad(nint scene, nint serializedObject, ProgressCallback progressCallback, nint progressCallbackUserData, out nint staticMesh);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplStaticMeshSave(nint staticMesh, nint serializedObject);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplStaticMeshAdd(nint staticMesh, nint scene);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplStaticMeshRemove(nint staticMesh, nint scene);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplInstancedMeshCreate(nint scene, ref InstancedMeshSettings settings, out nint instancedMesh);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplInstancedMeshRetain(nint instancedMesh);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplInstancedMeshRelease(ref nint instancedMesh);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplInstancedMeshAdd(nint instancedMesh, nint scene);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplInstancedMeshRemove(nint instancedMesh, nint scene);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplInstancedMeshUpdateTransform(nint instancedMesh, nint scene, Matrix4x4 transform);

        // HRTF

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplHRTFCreate(nint context, ref AudioSettings audioSettings, ref HRTFSettings hrtfSettings, out nint hrtf);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplHRTFRetain(nint hrtf);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplHRTFRelease(ref nint hrtf);

        // Probes

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplProbeArrayCreate(nint context, out nint probeArray);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplProbeArrayRetain(nint probeArray);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplProbeArrayRelease(ref nint probeArray);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplProbeArrayGenerateProbes(nint probeArray, nint scene, ref ProbeGenerationParams generationParams);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern int iplProbeArrayGetNumProbes(nint probeArray);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Sphere iplProbeArrayGetProbe(nint probeArray, int index);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplProbeBatchCreate(nint context, out nint probeBatch);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplProbeBatchRetain(nint probeBatch);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplProbeBatchRelease(ref nint probeBatch);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplProbeBatchLoad(nint context, nint serializedObject, out nint probeBatch);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplProbeBatchSave(nint probeBatch, nint serializedObject);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern int iplProbeBatchGetNumProbes(nint probeBatch);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplProbeBatchAddProbe(nint probeBatch, Sphere probe);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplProbeBatchAddProbeArray(nint probeBatch, nint probeArray);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplProbeBatchRemoveProbe(nint probeBatch, int index);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplProbeBatchCommit(nint probeBatch);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplProbeBatchRemoveData(nint probeBatch, ref BakedDataIdentifier identifier);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nuint iplProbeBatchGetDataSize(nint probeBatch, ref BakedDataIdentifier identifier);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplProbeBatchGetEnergyField(nint probeBatch, ref BakedDataIdentifier identifier, int probeIndex, nint energyField);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplProbeBatchGetReverb(nint probeBatch, ref BakedDataIdentifier identifier, int probeIndex, float[] reverbTimes);

        // Baking

        [DllImport(Library, EntryPoint = "iplReflectionsBakerBake", CallingConvention = CallingConvention.Cdecl)]
        public static extern void iplReflectionsBakerBake(nint context, ref ReflectionsBakeParams bakeParams, ProgressCallback progressCallback, nint userData);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplReflectionsBakerCancelBake(nint context);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplPathBakerBake(nint context, ref PathBakeParams bakeParams, ProgressCallback progressCallback, nint userData);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplPathBakerCancelBake(nint context);

        // Run-Time Simulation

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplSimulatorCreate(nint context, ref SimulationSettings settings, out nint simulator);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplSimulatorRetain(nint simulator);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSimulatorRelease(ref nint simulator);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSimulatorSetScene(nint simulator, nint scene);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSimulatorAddProbeBatch(nint simulator, nint probeBatch);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSimulatorRemoveProbeBatch(nint simulator, nint probeBatch);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSimulatorSetSharedInputs(nint simulator, SimulationFlags flags, ref SimulationSharedInputs sharedInputs);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSimulatorCommit(nint simulator);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSimulatorRunDirect(nint simulator);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSimulatorRunReflections(nint simulator);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSimulatorRunPathing(nint simulator);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplSourceCreate(nint simulator, ref SourceSettings settings, out nint source);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplSourceRetain(nint source);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSourceRelease(ref nint source);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSourceAdd(nint source, nint simulator);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSourceRemove(nint source, nint simulator);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSourceSetInputs(nint source, SimulationFlags flags, ref SimulationInputs inputs);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplSourceGetOutputs(nint source, SimulationFlags flags, ref SimulationOutputs outputs);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern float iplDistanceAttenuationCalculate(nint context, Vector3 source, Vector3 listener, ref DistanceAttenuationModel model);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplAirAbsorptionCalculate(nint context, Vector3 source, Vector3 listener, ref AirAbsorptionModel mode, float[] minDistances);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern float iplDirectivityCalculate(nint context, CoordinateSpace3 source, Vector3 listener, ref Directivity model);

        // Energy Field API

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplEnergyFieldCreate(nint context, ref EnergyFieldSettings settings, out nint energyField);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplEnergyFieldRetain(nint energyField);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplEnergyFieldRelease(ref nint energyField);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern int iplEnergyFieldGetNumChannels(nint energyField);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern int iplEnergyFieldGetNumBins(nint energyField);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplEnergyFieldGetData(nint energyField);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplEnergyFieldGetChannel(nint energyField, int channelIndex);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplEnergyFieldGetBand(nint energyField, int channelIndex, int bandIndex);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplEnergyFieldReset(nint energyField);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplEnergyFieldCopy(nint srcEnergyField, nint dstEnergyField);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplEnergyFieldSwap(nint energyFieldA, nint energyFieldB);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplEnergyFieldAdd(nint energyField1, nint energyField2, nint outEnergyField);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplEnergyFieldScale(nint inEnergyField, float scalar, nint outEnergyField);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplEnergyFieldScaleAccum(nint inEnergyField, float scalar, nint outEnergyField);

        // Impulse Response API

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern Error iplImpulseResponseCreate(nint context, ref ImpulseResponseSettings settings, out nint impulseResponse);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplImpulseResponseRetain(nint impulseResponse);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplImpulseResponseRelease(ref nint impulseResponse);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern int iplImpulseResponseGetNumChannels(nint impulseResponse);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern int iplImpulseResponseGetNumSamples(nint impulseResponse);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplImpulseResponseGetData(nint impulseResponse);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplImpulseResponseGetChannel(nint impulseResponse, int channelIndex);
#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplImpulseResponseReset(nint impulseResponse);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplImpulseResponseCopy(nint srcImpulseReponse, nint dstImpulseResponse);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplImpulseResponseSwap(nint impulseResponse1, nint impulseResponse2);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplImpulseResponseAdd(nint inImpulseResponse1, nint inImpulseResponse2, nint outImpulseResponse);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplImpulseResponseScale(nint inImpulseResponse, float scalar, nint outImpulseResponse);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplImpulseResponseScaleAccum(nint inImpulseRespnse, float scalar, nint outImpulseResponse);

        // Reconstructor API

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]

#endif
        public static extern Error iplReconstructorCreate(nint context, ref ReconstructorSettings settings, out nint reconstructor);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern nint iplReconstructorRetain(nint reconstructor);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplReconstructorRelease(ref nint reconstructor);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport(Library)]
#endif
        public static extern void iplReconstructorReconstruct(nint reconstructor, int numInputs, ref ReconstructorInputs inputs, ref ReconstructorSharedInputs sharedInputs, ref ReconstructorOutputs outputs);

        // UNITY PLUGIN

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport("audioplugin_phonon")]
#endif
        public static extern void iplUnityInitialize(nint context);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport("audioplugin_phonon")]
#endif
        public static extern void iplUnitySetPerspectiveCorrection(PerspectiveCorrection correction);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport("audioplugin_phonon")]
#endif
        public static extern void iplUnitySetHRTF(nint hrtf);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport("audioplugin_phonon")]
#endif
        public static extern void iplUnitySetSimulationSettings(SimulationSettings simulationSettings);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport("audioplugin_phonon")]
#endif
        public static extern void iplUnitySetReverbSource(nint reverbSource);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport("audioplugin_phonon")]
#endif
        public static extern int iplUnityAddSource(nint source);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport("audioplugin_phonon")]
#endif
        public static extern void iplUnityRemoveSource(int handle);

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport("audioplugin_phonon")]
#endif
        public static extern void iplUnityTerminate();

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        [DllImport("__Internal")]
#else
        [DllImport("audioplugin_phonon")]
#endif
        public static extern void iplUnitySetHRTFDisabled(bool disabled);
    }
}
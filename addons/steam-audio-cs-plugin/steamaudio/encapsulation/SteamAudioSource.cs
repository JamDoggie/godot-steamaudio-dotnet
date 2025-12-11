using FMOD;
using FMOD.Studio;
using Godot;
using SteamAudioDotnet.scripts.nativelib;
using SteamAudioDotnet.scripts.steamaudio.extension;
using System;
using Error = SteamAudio.Error; 
using SteamAudio;
using SteamAudioDotnet.scripts.steamaudio.nodes;

namespace SteamAudioDotnet.scripts.steamaudio.encapsulation
{
    public class SteamAudioSource : SteamAudioObject
    {
        public int? FmodSourceHandle = null;
        public EventInstance? FmodEvent = null;

        public string FmodGuid { get; internal set; } = string.Empty; 

        // If there is no FMOD source attached, this is treated as a reverb listener.
        public bool IsReverbListener => FmodEvent == null || FmodSourceHandle == null;

        public Transform3D SourceTransform { get; set; } = Transform3D.Identity;

        public SteamAudioSource(FmodSteamAudioBridge steamAudio, EventInstance eventInstance)
        {
            CreateSource(steamAudio, eventInstance);
        }

        public SteamAudioSource(FmodSteamAudioBridge steamAudio)
        {
            CreateSource(steamAudio, null);
        }

        ~SteamAudioSource() 
        { 
            if (Ptr != nint.Zero)
            {
                API.iplSourceRelease(ref Ptr);
            }
        }

        public unsafe void UpdateSourceInputs(
            FmodSteamAudioBridge steamAudio, IntPtr simulator, SimulationFlags flagsToSimulate)
        {
            // Early return if this is for pathing simulation and there is no pathing data loaded.
            if (steamAudio.AudioBaker == null || steamAudio.AudioBaker.PathingProbeBatchPtr == IntPtr.Zero)
            {
                if (flagsToSimulate.HasFlag(SimulationFlags.Pathing))
                {
                    GD.PrintErr("No pathing data loaded; cannot simulate pathing for source.");
                    return;
                }
            }

            SteamAudioStaticSourceMarker? staticMarker = null;

            lock (FmodSteamAudioBridge.AudioSourcesLock)
            {
                foreach (SteamAudioStaticSourceMarker marker in FmodSteamAudioBridge.StaticAudioSources)
                {
                    if (marker.GlobalTransformThreadSafe.IsEqualApprox(SourceTransform))
                    {
                        staticMarker = marker;
                        break;
                    }
                }
            }
            
            bool useBaked = steamAudio.BakedDataLoaded;

            SimulationInputs inputs = new()
            {
                flags = steamAudio.SimFlags,
                directFlags =
                DirectSimulationFlags.Occlusion
                | DirectSimulationFlags.Transmission
                | DirectSimulationFlags.AirAbsorption
                | DirectSimulationFlags.Directivity
                | DirectSimulationFlags.DistanceAttenuation,
                occlusionType = OcclusionType.Raycast,
                occlusionRadius = 4.0f,
                numOcclusionSamples = 32,
                hybridReverbOverlapPercent = 0.25f,
                hybridReverbTransitionTime = 1.0f,
                numTransmissionRays = 1,
                baked = useBaked ? Bool.True : Bool.False,
                pathingProbes = IntPtr.Zero,
                pathingOrder = 0,
                enableValidation = Bool.False,
                findAlternatePaths = Bool.False,
                visRange = 1000f,
                visThreshold = 0.1f,
                visRadius = 1.0f,
                
                deviationModel = (nint)steamAudio.DeviationModel,
            };

            if (!IsReverbListener && FmodEvent != null)
            {
                if (!FmodSteamAudioBridge.CheckFmodErr(FmodEvent.Value.get3DAttributes(out ATTRIBUTES_3D attribs), RESULT.ERR_INVALID_HANDLE))
                    return;

                FmodSteamAudioBridge.IplCoordsFromFmod(attribs, ref inputs.source);

                SourceTransform = inputs.source.AsGodotTransform();
            }
            else
            {
                // Source transform will be externally set in this case since this is the listener source.
                inputs.source = SourceTransform.AsSteamAudioTransform();
                inputs.directFlags = 0;
                inputs.flags = SimulationFlags.Reflections;
                inputs.baked = Bool.False;
            }

            inputs.airAbsorptionModel.type = AirAbsorptionModelType.Default;
            inputs.distanceAttenuationModel.type = DistanceAttenuationModelType.Default;

            inputs.reverbScaleLow = 1.0f;
            inputs.reverbScaleMid = 1.0f;
            inputs.reverbScaleHigh = 1.0f;

            if (steamAudio.BakedDataLoaded && steamAudio.AudioBaker != null)
            {
                // Load pathing data
                if (flagsToSimulate.HasFlag(SimulationFlags.Pathing))
                {
                    inputs.baked = Bool.True;
                    inputs.bakedDataIdentifier.type = BakedDataType.Pathing;
                    inputs.bakedDataIdentifier.variation = BakedDataVariation.Dynamic;
                    inputs.pathingProbes = steamAudio.AudioBaker.PathingProbeBatchPtr;
                    inputs.pathingOrder = 1;
                }
                // Load reflection/reverb data
                else
                {
                    inputs.bakedDataIdentifier.type = BakedDataType.Reflections;
                    inputs.bakedDataIdentifier.variation = BakedDataVariation.Reverb;

                    if (staticMarker != null)
                    {
                        inputs.bakedDataIdentifier.variation = BakedDataVariation.StaticSource;

                        inputs.bakedDataIdentifier.endpointInfluence.center.x = staticMarker.GlobalTransformThreadSafe.Origin.X;
                        inputs.bakedDataIdentifier.endpointInfluence.center.y = staticMarker.GlobalTransformThreadSafe.Origin.Y;
                        inputs.bakedDataIdentifier.endpointInfluence.center.z = staticMarker.GlobalTransformThreadSafe.Origin.Z;
                        inputs.bakedDataIdentifier.endpointInfluence.radius = steamAudio.AudioBaker.StaticSourceInfluenceRadius;
                    }

                    if (staticMarker == null)
                    {
                        inputs.baked = Bool.False;
                    }
                }
            }

            API.iplSourceSetInputs(Ptr, flagsToSimulate, ref inputs);
        }

        private void CreateSource(FmodSteamAudioBridge steamAudio, EventInstance? eventInstance)
        {
            SourceSettings sourceSettings = new()
            {
                flags = steamAudio.SourceFlags
            };

            IntPtr sourcePtr;

            Error error = API.iplSourceCreate(steamAudio.Simulator, ref sourceSettings, out sourcePtr);

            #region Source error checking
            if (error != Error.Success)
            {
                GD.PrintErr("Failed to create Steam Audio source: " + error);
                Ptr = IntPtr.Zero;
                return;
            }

            if (sourcePtr == IntPtr.Zero)
            {
                GD.PrintErr("Steam Audio source handle is null... somehow.");
                Ptr = IntPtr.Zero;
                return;
            }
            #endregion

            if (eventInstance != null)
            {
                FmodEvent = eventInstance;
            }

            // Add the source to Steam Audio and FMOD.
            API.iplSourceAdd(sourcePtr, steamAudio.Simulator);
            FmodSourceHandle = SteamFmodApi.iplFMODAddSource(sourcePtr);

            API.iplSimulatorCommit(steamAudio.Simulator);

            Ptr = sourcePtr;

            if (eventInstance != null && IsValid)
            {
                if (!FmodSteamAudioBridge.CheckFmodErr(eventInstance.Value.getDescription(out EventDescription eventDesc)))
                    return;

                if (!FmodSteamAudioBridge.CheckFmodErr(eventDesc.getID(out GUID eventGuid)))
                    return;

                string guidString = eventGuid.FmodGuidToString();

                FmodGuid = guidString;
            }
        }

        public void UpdateCustomOcclusion(float occlusionValue)
        {
            if (FmodEvent == null || !FmodEvent.Value.isValid())
                return;

            DSP? steamDSP = FmodSteamAudioBridge.FmodGetSteamDSP(FmodEvent.Value);

            if (steamDSP == null)
                return;

            FmodSteamAudioBridge.CheckFmodErr(steamDSP.Value.setParameterFloat((int)IPLSpatializerParams.IPL_SPATIALIZE_OCCLUSION, occlusionValue));
        }
    }
}

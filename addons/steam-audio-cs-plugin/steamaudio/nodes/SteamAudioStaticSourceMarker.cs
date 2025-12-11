using Godot;
using SteamAudioDotnet.scripts.nativelib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamAudioDotnet.scripts.steamaudio.nodes
{
    [GlobalClass, Tool]
    public partial class SteamAudioStaticSourceMarker : Node3D
    {
        private volatile object transformLock = new object();
        private Transform3D globalTransform = Transform3D.Identity;

        public Transform3D GlobalTransformThreadSafe
        {
            get
            {
                lock (transformLock)
                {
                    return globalTransform;
                }
            }

            set
            {
                lock (transformLock)
                {
                    globalTransform = value;
                }
            }
        }

        public override void _Ready()
        {
            GlobalTransformThreadSafe = GlobalTransform;
        }

        public override void _EnterTree()
        {
            if (!Engine.IsEditorHint())
            {
                lock (FmodSteamAudioBridge.AudioSourcesLock)
                    FmodSteamAudioBridge.StaticAudioSources.Add(this);
            }
                
        }

        public override void _ExitTree()
        {
            if (!Engine.IsEditorHint())
            {
                lock (FmodSteamAudioBridge.AudioSourcesLock)
                    FmodSteamAudioBridge.StaticAudioSources.Remove(this);
            }
                
        }
    }
}

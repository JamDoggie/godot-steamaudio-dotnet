using Godot;
using SteamAudioDotnet.scripts.steamaudio.encapsulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamAudioDotnet.scripts.steamaudio.soundprocessors
{
    public abstract class OcclusionProcessor
    {
        public abstract float ProcessOcclusion(World3D world, SteamAudioSource source, Node3D listener);
    }
}

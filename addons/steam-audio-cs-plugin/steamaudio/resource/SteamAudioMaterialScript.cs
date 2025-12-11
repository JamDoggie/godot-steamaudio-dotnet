using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamAudioDotnet.scripts.steamaudio.resource
{
    [GlobalClass, Tool]
    public partial class SteamAudioMaterialScript : StandardMaterial3D
    {
        [Export]
        public SteamAudioMaterialResource? SteamAudioMaterial = null;
    }
}

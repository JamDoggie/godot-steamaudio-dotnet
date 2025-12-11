using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamAudioDotnet.scripts.steamaudio.encapsulation
{
    public abstract class SteamAudioObject
    {
        public IntPtr Ptr;

        public bool IsValid => Ptr != IntPtr.Zero;
    }
}

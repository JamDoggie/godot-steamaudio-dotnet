using Godot;
using Godot.Collections;
using SteamAudio;

namespace SteamAudioDotnet.scripts.steamaudio.resource
{
    [GlobalClass, Tool]
    public partial class SteamAudioBakeResults : Resource
    {
        [Export]
        public Dictionary<SteamAudioBakeIdentifier, byte[]> ProbeBatches { get; set; } = [];
        [Export]
        public ReflectionsBakeFlags BakeFlags;

        public SteamAudioBakeResults(ReflectionsBakeFlags bakeFlags)
        {
            BakeFlags = bakeFlags;
        }

        public SteamAudioBakeResults() { }
    }
}

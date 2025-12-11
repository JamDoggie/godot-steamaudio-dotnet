using Godot;

namespace SteamAudioDotnet.scripts.steamaudio.resource
{
    [GlobalClass, Tool]
    public partial class SteamAudioBakeIdentifier : Resource
    {
        [Export]
        public SteamAudio.BakedDataVariation Variation { get; set; } = SteamAudio.BakedDataVariation.Reverb;
        [Export]
        public Vector3 Origin { get; set; } = Vector3.Zero;
        [Export]
        public float Radius { get; set; } = 1.0f;
    }
}

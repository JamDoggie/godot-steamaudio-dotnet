using Godot;

namespace SteamAudioDotnet.scripts.steamaudio.resource
{
    [GlobalClass, Tool]
    public partial class SteamAudioMaterialResource : Resource
    {
        [Export]
        public float AbsorptionLowFreq { get; set; } = 0.1f;
        [Export]
        public float AbsorptionMidFreq { get; set; } = 0.2f;
        [Export]
        public float AbsorptionHighFreq { get; set; } = 0.3f;
        [Export]
        public float Scattering { get; set; } = 0.05f;
        [Export]
        public float TransmissionLowFreq { get; set; } = 0.1f;
        [Export]
        public float TransmissionMidFreq { get; set; } = 0.05f;
        [Export]
        public float TransmissionHighFreq { get; set; } = 0.03f;
    }
}

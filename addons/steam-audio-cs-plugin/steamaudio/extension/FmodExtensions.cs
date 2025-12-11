using FMOD;
using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace SteamAudioDotnet.scripts.steamaudio.extension
{
    public static class FmodExtensions
    {
        internal unsafe static string FmodGuidToString(this GUID guid)
        {
            int structSize = Marshal.SizeOf<GUID>();

            if (structSize < sizeof(int) * 4)
                throw new InvalidOperationException("GUID struct too small.");

            byte* bytes = stackalloc byte[structSize];

            Marshal.StructureToPtr(guid, (nint)bytes, false);

            uint data1 = BinaryPrimitives.ReadUInt32LittleEndian(new Span<byte>(bytes, 4));
            ushort data2 = BinaryPrimitives.ReadUInt16LittleEndian(new Span<byte>(bytes + 4, 2));
            ushort data3 = BinaryPrimitives.ReadUInt16LittleEndian(new Span<byte>(bytes + 6, 2));

            // bytes[8]..bytes[15] == Data4[0..7]
            return string.Format("{{{0:x8}-{1:x4}-{2:x4}-{3:x2}{4:x2}-{5:x2}{6:x2}{7:x2}{8:x2}{9:x2}{10:x2}}}",
                data1, data2, data3,
                bytes[8], bytes[9],
                bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15]);
        }
    }
}

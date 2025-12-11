using Godot;
using SteamAudio;
using System;
using System.Runtime.InteropServices;

namespace SteamAudioDotnet.scripts.steamaudio.encapsulation
{
    public class SteamAudioSerializedObject : SteamAudioObject
    {
        public nuint Size => API.iplSerializedObjectGetSize(Ptr);

        public nint Data => API.iplSerializedObjectGetData(Ptr);

        private nint customDataPointer = IntPtr.Zero;

        public SteamAudioSerializedObject(IntPtr context)
        {
            if (context == IntPtr.Zero)
                GD.PrintErr($"Couldn't create {GetType().Name}, context was null!");

            var serializedObjectSettings = new SerializedObjectSettings { };

            API.iplSerializedObjectCreate(context, ref serializedObjectSettings, out nint serializedObject);

            Ptr = serializedObject;
        }

        public SteamAudioSerializedObject(IntPtr context, Span<byte> data)
        {
            if (context == IntPtr.Zero)
                GD.PrintErr($"Couldn't create {GetType().Name}, context was null!");

            customDataPointer = Marshal.AllocHGlobal(data.Length);

            Marshal.Copy(data.ToArray(), 0, customDataPointer, data.Length);

            var serializedObjectSettings = new SerializedObjectSettings 
            { 
                data = customDataPointer,
                size = (nuint)data.Length
            };

            API.iplSerializedObjectCreate(context, ref serializedObjectSettings, out nint serializedObject);

            Ptr = serializedObject;
        }

        ~SteamAudioSerializedObject()
        {
            if (IsValid)
                API.iplSerializedObjectRelease(ref Ptr);

            if (customDataPointer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(customDataPointer);
                customDataPointer = IntPtr.Zero;
            }
        }
    }
}

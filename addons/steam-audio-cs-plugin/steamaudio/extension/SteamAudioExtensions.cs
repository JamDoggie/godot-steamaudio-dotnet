using Godot;
using SteamAudioDotnet.scripts.nativelib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamAudio;

namespace SteamAudioDotnet.scripts.steamaudio.extension
{
    public static class SteamAudioExtensions
    {
        public static Transform3D AsGodotTransform(this CoordinateSpace3 iplTransform)
        {
            Godot.Vector3 up = new(iplTransform.up.x, iplTransform.up.y, iplTransform.up.z);
            Godot.Vector3 right = new(iplTransform.right.x, iplTransform.right.y, iplTransform.right.z);
            Godot.Vector3 ahead = new(iplTransform.ahead.x, iplTransform.ahead.y, iplTransform.ahead.z);

            Godot.Vector3 origin = new(iplTransform.origin.x, iplTransform.origin.y, iplTransform.origin.z);

            Transform3D transform = new(new Basis(right, up, -ahead), origin);

            return transform;
        }

        public static CoordinateSpace3 AsSteamAudioTransform(this Transform3D godotTransform)
        {
            SteamAudio.Vector3 right = FmodSteamAudioBridge.IplVector(godotTransform.Basis.X);
            SteamAudio.Vector3 up = FmodSteamAudioBridge.IplVector(godotTransform.Basis.Y);
            SteamAudio.Vector3 ahead = FmodSteamAudioBridge.IplVector(-godotTransform.Basis.Z);

            SteamAudio.Vector3 origin = FmodSteamAudioBridge.IplVector(godotTransform.Origin);

            CoordinateSpace3 coordSpace = new()
            {
                right = right,
                up = up,
                ahead = ahead,
                origin = origin
            };

            return coordSpace;
        }

        public static Matrix4x4 AsSteamAudioMatrix(this Transform3D godotTransform)
        {
            Matrix4x4 matrix = new Matrix4x4();
            matrix.m00 = godotTransform.Basis.X.X;
            matrix.m01 = godotTransform.Basis.Y.X;
            matrix.m02 = -godotTransform.Basis.Z.X;
            matrix.m03 = godotTransform.Origin.X;

            matrix.m10 = godotTransform.Basis.X.Y;
            matrix.m11 = godotTransform.Basis.Y.Y;
            matrix.m12 = -godotTransform.Basis.Z.Y;
            matrix.m13 = godotTransform.Origin.Y;

            matrix.m20 = godotTransform.Basis.X.Z;
            matrix.m21 = godotTransform.Basis.Y.Z;
            matrix.m22 = -godotTransform.Basis.Z.Z;
            matrix.m23 = godotTransform.Origin.Z;

            matrix.m30 = 0f;
            matrix.m31 = 0f;
            matrix.m32 = 0f;
            matrix.m33 = 1f;

            return matrix;
        }
    }
}

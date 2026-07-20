using System.Numerics;
using System.Runtime.InteropServices;

namespace Geo.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ColorBufferData
    {
        // float4 в HLSL строго соответствует Vector4 в C# (16 байт)
        public Vector4 MarkerColor;
    }
}

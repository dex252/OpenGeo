using System.Numerics;
using System.Runtime.InteropServices;

namespace Geo.Graphics
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct TransformBuffer
    {
        public System.Numerics.Matrix4x4 WorldViewProj; // Чистые 64 байта
    }
}

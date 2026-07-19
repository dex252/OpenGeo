using System.Numerics;
using System.Runtime.InteropServices;

namespace Geo.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TransformBuffer
    {
        public Matrix4x4 WorldViewProj; // Матрица трансформации 4х4
    }
}

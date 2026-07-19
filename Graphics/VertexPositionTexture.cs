using System.Numerics;
using System.Runtime.InteropServices;

namespace Geo.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionTexture
    {
        /// <summary>
        /// 3D-координаты (X, Y, Z)
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// 2D-координаты на карте (U, V)
        /// </summary>
        public Vector2 TextureCoordinate;

        public VertexPositionTexture(Vector3 position, Vector2 textureCoordinate)
        {
            Position = position;
            TextureCoordinate = textureCoordinate;
        }
    }
}
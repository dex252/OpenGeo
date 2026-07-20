using System.Numerics;

namespace Geo.Graphics
{
    public static class MeshFactory
    {
        /// <summary>
        /// Генерирует вершины и индексы для центрированного куба (маркера) заданного размера.
        /// </summary>
        public static (VertexPositionTexture[] Vertices, uint[] Indices) CreateCube(float size)
        {
            var vertices = new VertexPositionTexture[]
            {
                // Передняя грань (локальные координаты относительно центра 0,0,0)
                new VertexPositionTexture(new Vector3(-size, -size,  size), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3( size, -size,  size), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3( size,  size,  size), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-size,  size,  size), new Vector2(0, 1)),
                
                // Задняя грань
                new VertexPositionTexture(new Vector3(-size, -size, -size), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3( size, -size, -size), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3( size,  size, -size), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-size,  size, -size), new Vector2(0, 1)),
            };

            var indices = new uint[]
            {
                0, 2, 1,   0, 3, 2, // Перед
                4, 5, 6,   4, 6, 7, // Назад
                3, 6, 2,   3, 7, 6, // Верх
                0, 1, 5,   0, 5, 4, // Низ
                1, 2, 6,   1, 6, 5, // Право
                0, 4, 7,   0, 7, 3  // Лево
            };

            return (vertices, indices);
        }

        // В будущем сюда можно будет добавить методы CreateCone (для армий), 
        // CreateLine (для торговых путей/границ) и т.д.
    }
}


using Geo.Graphics;
using System.Numerics;

namespace Geo.Math
{
    public static class SphereGenerator
    {
        public static unsafe (VertexPositionTexture[] Vertices, uint[] Indices) Generate(float radius, int slices, int stacks)
        {
            var vertices = new List<VertexPositionTexture>();
            var indices = new List<uint>();

            // Перебираем сферу сверху вниз (от Северного полюса к Южному)
            for (int stack = 0; stack <= stacks; stack++)
            {
                // phi — это угол наклона (широта)
                float phi = MathF.PI * stack / stacks;
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);

                // v — это вертикальная координата на текстуре (0 — верх, 1 — низ)
                float v = (float)stack / stacks;

                // Идем по кругу (по долготе)
                for (int slice = 0; slice <= slices; slice++)
                {
                    // theta — это угол поворота по кругу (долгота)
                    float theta = 2.0f * MathF.PI * slice / slices;

                    // u — это горизонтальная координата на текстуре (от 0 до 1 по кругу)
                    float u = (float)slice / slices;

                    // Тригонометрическая формула перевода углов в 3D-координаты
                    float x = radius * sinPhi * MathF.Cos(theta);
                    float y = radius * cosPhi;
                    float z = radius * sinPhi * MathF.Sin(theta);

                    // Сохраняем готовую точку: где она в 3D и какой пиксель карты на ней будет
                    vertices.Add(new VertexPositionTexture(new Vector3(x, y, z), new Vector2(u, v)));
                }
            }

            //Сборка треугольников (Индексов)
            for (int stack = 0; stack < stacks; stack++)
            {
                for (int slice = 0; slice < slices; slice++)
                {
                    // Находим индексы четырех углов текущей ячейки сетки
                    uint first = (uint)((stack * (slices + 1)) + slice);
                    uint second = (uint)(first + slices + 1);

                    // Первый треугольник ячейки (из трех точек)
                    indices.Add(first);
                    indices.Add(second);
                    indices.Add(first + 1);

                    // Второй треугольник ячейки (из трех точек)
                    indices.Add(first + 1);
                    indices.Add(second);
                    indices.Add(second + 1);
                }
            }

            return (vertices.ToArray(), indices.ToArray());
        }
    }
}

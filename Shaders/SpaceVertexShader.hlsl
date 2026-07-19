struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float2 ScreenPos : TEXCOORD0; // Передаем координаты экрана для генерации звезд
};

// Метод принимает системный идентификатор вершины SV_VertexID (от 0u до 3u)
PS_INPUT main(uint vertexID : SV_VertexID)
{
    PS_INPUT output;

    // Магическая геймдев-формула: превращаем ID вершины в координаты углов экрана (-1 до 1)
    // vertexID = 0 -> (-1, -1) [Левый нижний]
    // vertexID = 1 -> (-1,  1) [Левый верхний]
    // vertexID = 2 -> ( 1, -1) [Правый нижний]
    // vertexID = 3 -> ( 1,  1) [Правый верхний]
    float2 texCoord = float2((vertexID << 1) & 2, vertexID & 2);
    output.Position = float4(texCoord * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f), 0.0f, 1.0f);

    // Сохраняем чистые координаты для генератора псевдослучайных чисел
    output.ScreenPos = output.Position.xy;

    return output;
}

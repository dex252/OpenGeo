// Структура на входе (из нашей структуры VertexPositionTexture в C#)
struct VS_INPUT
{
    float3 Position : POSITION;
    float2 TexCoord : TEXCOORD;
};

// Структура на выходе (передается в пиксельный шейдер)
struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD;
};

PS_INPUT main(VS_INPUT input)
{
    PS_INPUT output;

    // Переводим 3D-координаты в 4D-вектор для DirectX.
    // Делим X и Y на 2.5, чтобы сфера уменьшилась и полностью влезла в экран.
    output.Position = float4(input.Position.x / 2.5f, input.Position.y / 2.5f, input.Position.z, 1.0f);

    // Просто пробрасываем текстурные координаты дальше
    output.TexCoord = input.TexCoord;

    return output;
}

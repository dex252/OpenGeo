// Регистрируем буфер трансформации в слоте b0
cbuffer TransformBuffer : register(b0)
{
    matrix worldViewProj;
};

struct VS_INPUT
{
    float3 Position : POSITION;
    float2 TexCoord : TEXCOORD;
};

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD;
};

PS_INPUT main(VS_INPUT input)
{
    PS_INPUT output;

    // Умножаем 3D-координату вершины на матрицу WVP
    // Метод mul — это встроенное в HLSL матричное умножение
    output.Position = mul(float4(input.Position, 1.0f), worldViewProj);

    // Пробрасываем текстурные координаты
    output.TexCoord = input.TexCoord;

    return output;
}

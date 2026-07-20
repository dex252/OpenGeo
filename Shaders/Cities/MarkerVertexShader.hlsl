cbuffer TransformBuffer : register(b0)
{
    matrix worldViewProj;
};

struct VS_INPUT
{
    float3 Position : POSITION;
};

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float3 LocalPos : TEXCOORD0; // <-- Передаем локальную 3D-позицию точки куба
};

PS_INPUT main(VS_INPUT input)
{
    PS_INPUT output;
    output.Position = mul(float4(input.Position, 1.0f), worldViewProj);
    output.LocalPos = input.Position; // Просто копируем локальные X, Y, Z
    return output;
}

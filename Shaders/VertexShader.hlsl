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
    output.Position = mul(float4(input.Position, 1.0f), worldViewProj);
    output.TexCoord = float2(1.0f - input.TexCoord.x, input.TexCoord.y);
    return output;
}

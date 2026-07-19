// Принимаем текстуру Земли из C# (зарегистрирована в слоте t0)
Texture2D EarthTexture : register(t0);
// Принимаем настройки сглаживания (зарегистрированы в слоте s0)
SamplerState TextureSampler : register(s0);

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD;
};

float4 main(PS_INPUT input) : SV_TARGET
{
    return EarthTexture.Sample(TextureSampler, input.TexCoord);
}

Texture2D EarthTexture : register(t0);       // Карта земли (слот 0)
Texture2D CityTextTexture : register(t1);    // НАШ ТЕКСТОВЫЙ СЛОЙ (слот 1)
SamplerState TextureSampler : register(s0);

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD;
};

float4 main(PS_INPUT input) : SV_TARGET
{
    // 1. Считываем базовый цвет планеты из 8K текстуры
    float4 earthColor = EarthTexture.Sample(TextureSampler, input.TexCoord);

// 2. Считываем пиксель текста из нашего сгенерированного слоя
float4 textColor = CityTextTexture.Sample(TextureSampler, input.TexCoord);

// 3. Магия смешивания (Alpha Blending):
// Если в этой точке текстуры есть буква (Alpha > 0), мы плавно накладываем цвет буквы поверх Земли
float4 finalColor = lerp(earthColor, textColor, textColor.a);

return finalColor;
}

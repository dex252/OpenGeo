Texture2D EarthTexture : register(t0);       // Карта земли (слот 0)
Texture2D CityTextTexture : register(t1);    // Наш готовый текстовый слой (слот 1)
SamplerState TextureSampler : register(s0);

cbuffer CameraBuffer : register(b1)
{
    float CameraRadius; // Текущее расстояние от C# (от 2.5 до 5.0)
    float3 Padding;
};

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD;
};

float4 main(PS_INPUT input) : SV_TARGET
{
    // 1. Считываем цвета пикселей из обеих текстур
    float4 earthColor = EarthTexture.Sample(TextureSampler, input.TexCoord);
    float4 textColor = CityTextTexture.Sample(TextureSampler, input.TexCoord);

    // 2. ВМЕСТО IF: Используем функцию smoothstep для создания маски видимости.
    // Если CameraRadius между 3.5 и 2.7, textVisibility плавно изменится от 0.0 до 1.0.
    // Это не только убирает ветвление if, но и делает появление текста потрясающе плавным (Fade-in эффект)!
    float textVisibility = smoothstep(3.5f, 2.7f, CameraRadius);

    // 3. Умножаем прозрачность самого текста на нашу маску зума
    float finalAlpha = textColor.a * textVisibility;

    // 4. Линейно смешиваем цвета. Если finalAlpha = 0 (космос) — вернется чистая Земля.
    // Процессор видеокарты выполняет эту строчку в один такт для всех пикселей параллельно.
    return lerp(earthColor, textColor, finalAlpha);
}

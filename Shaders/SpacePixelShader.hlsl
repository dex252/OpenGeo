struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float2 ScreenPos : TEXCOORD0;
};

// Функция генерации более качественного и хаотичного шума
float Hash21(float2 p)
{
    p = frac(p * float2(123.34f, 456.21f));
    p += dot(p, p + 45.32f);
    return frac(p.x * p.y);
}

float4 main(PS_INPUT input) : SV_TARGET
{
    // Глубокий космический цвет вакуума
    float3 finalColor = float3(0.005f, 0.005f, 0.015f);

// Слой 1: Мелкая и тусклая звездная пыль на заднем фоне
float2 grid1 = floor(input.ScreenPos * 1200.0f);
float n1 = Hash21(grid1);
if (n1 > 0.995f)
{
    finalColor += float3(0.15f, 0.15f, 0.2f) * frac(n1 * 10.0f);
}

// Слой 2: Крупные, редкие и яркие созвездия
float2 grid2 = floor(input.ScreenPos * 400.0f);
float n2 = Hash21(grid2);
if (n2 > 0.991f)
{
    // Делаем мерцание: синусоида, завязанная на случайный ID звезды
    float brightness = 0.5f + 0.5f * frac(n2 * 100.0f);

    // Редким звездам придаем легкий голубоватый или желтоватый оттенок
    float3 starColor = float3(1.0f, 1.0f, 1.0f);
    if (n2 > 0.997f) starColor = float3(0.8f, 0.9f, 1.0f); // Голубой гигант
    else if (n2 < 0.993f) starColor = float3(1.0f, 0.95f, 0.85f); // Желтый карлик

    finalColor += starColor * brightness;
}

return float4(finalColor, 1.0f);
}

cbuffer ColorBuffer : register(b1)
{
    float4 MarkerColor; // Базовый сочный цвет города из C#
};

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float3 LocalPos : TEXCOORD0; // Наша локальная высота от -0.5 до +0.5
};

float4 main(PS_INPUT input) : SV_TARGET
{
    // 1. Переводим высоту Y из диапазона (-0.5 .. 0.5) в чистый коэффициент градиента (0.0 .. 1.0)
    // В самом низу кубика (Y = -0.5) фактор будет равен 0.0
    // На самой вершине кубика (Y = +0.5) фактор будет равен 1.0
    float gradientFactor = input.LocalPos.y + 0.5f;

// 2. Делаем градиент чуть более мягким и нелинейным с помощью функции степени
// Значение 0.5f плавно растянет потемнение к основанию
gradientFactor = pow(gradientFactor, 0.5f);

// 3. Задаем цвет мягкого благородного затемнения у основания
// Вместо глухого черного цвета берем глубокий темный оттенок самого города
float4 darkBase = MarkerColor * 0.15f;
darkBase.a = 1.0f; // Сохраняем полную непрозрачность

// 4. Плавно смешиваем глубокий темный низ и яркий сочный верх
return lerp(darkBase, MarkerColor, gradientFactor);
}

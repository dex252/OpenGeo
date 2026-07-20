using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Geo.Gameplay.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;

namespace Geo.Graphics
{
    public static class TextAtlasGenerator
    {
        static int MAX_TRY_ATTEMPS = 10;
        static float STEP_ATTEMPS = 0.5f;

        /// <summary>
        /// Динамически генерирует слой текста, плавно подтягивая надписи вверх при наложениях.
        /// </summary>
        public static string GenerateCityTextLayer(WorldSimulation world, int textureWidth, int textureHeight, float fontSize = 14.0f)
        {
            // 1. Создаем абсолютно прозрачное изображение под разрешение карты (например, 4K)
            using (var image = new Image<Rgba32>(textureWidth, textureHeight, Color.Transparent))
            {
                SixLabors.Fonts.Font font = SystemFonts.CreateFont("Arial", fontSize, FontStyle.Bold);

                // Список для отслеживания прямоугольных зон, которые уже заняты текстом
                var occupiedAreas = new List<RectangleF>();

                image.Mutate(ctx =>
                {
                    foreach (var city in world.Cities)
                    {
                        // Переводим широту и долготу в пиксели текстуры
                        float pixelX = ((city.Longitude + 180.0f) / 360.0f) * textureWidth;
                        float pixelY = ((90.0f - city.Latitude) / 180.0f) * textureHeight;

                        // Начальная точка привязки текста (чуть правее и выше маркера города)
                        float currentLabelX = pixelX + 10;
                        float currentLabelY = pixelY - 5;

                        // Измеряем точные физические габариты текста в пикселях
                        var textOptions = new TextOptions(font);
                        FontRectangle size = TextMeasurer.MeasureAdvance(city.Name, textOptions);

                        // Формируем ограничивающий прямоугольник (Bounding Box) для текущей надписи
                        var currentLabelRect = new RectangleF(currentLabelX, currentLabelY, size.Width, size.Height);

                        // ====================================================================
                        // 2. АЛГОРИТМ ПЛАВНОГО СДВИГА (МАКСИМУМ 10 ПОПЫТОК ПО 0.5 ПИКСЕЛЯ)
                        // ====================================================================
                        int maxAttempts = MAX_TRY_ATTEMPS;
                        int attempt = 0;
                        bool isOverlapping = true;

                        while (isOverlapping && attempt < maxAttempts)
                        {
                            isOverlapping = false;

                            // Проверяем пересечение со всеми городами, которые нарисованы до нас
                            foreach (var occupiedRect in occupiedAreas)
                            {
                                if (currentLabelRect.IntersectsWith(occupiedRect))
                                {
                                    isOverlapping = true;

                                    // Плавно подтягиваем текст вверх (к северу) на 0.5 пикселя за шаг
                                    currentLabelY -= STEP_ATTEMPS;

                                    // Пересчитываем положение прямоугольника на новой высоте
                                    currentLabelRect = new RectangleF(currentLabelX, currentLabelY, size.Width, size.Height);

                                    // Прерываем foreach, чтобы начать проверку заново по всему списку занятых зон
                                    break;
                                }
                            }

                            attempt++;
                        }

                        // Сохраняем финальную зону в список занятых, чтобы следующие города её учитывали
                        // Если 10 попыток не хватило, город все равно добавится в базу и нарисуется "как есть"
                        occupiedAreas.Add(currentLabelRect);

                        // ====================================================================
                        // 3. ОТРИСОВКА ВЫЧИСЛЕННОЙ НАДПИСИ
                        // ====================================================================
                        var c = city.VisualColor;
                        var textColor = Color.FromRgba((byte)(c.X * 255), (byte)(c.Y * 255), (byte)(c.Z * 255), (byte)(c.W * 255));

                        // Итоговая точка для вывода букв
                        var finalPoint = new PointF(currentLabelRect.X, currentLabelRect.Y);

                        // Рисуем мягкую черную подложку-тень для контраста на полярном льду
                        ctx.DrawText(city.Name, font, Color.Black, new PointF(finalPoint.X + 1.0f, finalPoint.Y + 1.0f));

                        // Рисуем основной текст уникальным цветом города
                        ctx.DrawText(city.Name, font, textColor, finalPoint);
                    }
                });

                // Экспортируем получившийся слой в папку Assets проекта
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string projectDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\"));
                string outputPath = Path.Combine(projectDir, "Assets", "cities_text_layer.png");

                image.SaveAsPng(outputPath);
                return outputPath;
            }
        }
    }
}

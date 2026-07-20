using System;
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
        /// <summary>
        /// Генерирует прозрачный PNG слой с названиями городов.
        /// </summary>
        /// <param name="fontSize">Размер шрифта. Передайте 5.0f - 6.0f, чтобы уменьшить текст в 5 раз.</param>
        public static string GenerateCityTextLayer(WorldSimulation world, int textureWidth, int textureHeight, float fontSize = 6.0f)
        {
            // Создаем абсолютно прозрачную картинку нужного разрешения
            using (var image = new Image<Rgba32>(textureWidth, textureHeight, Color.Transparent))
            {
                // Создаем шрифт с регулируемым размером
                SixLabors.Fonts.Font font = SystemFonts.CreateFont("Arial", fontSize, FontStyle.Bold);

                image.Mutate(ctx =>
                {
                    foreach (var city in world.Cities)
                    {
                        // Переводим координаты в пиксели текстуры
                        float pixelX = ((city.Longitude + 180.0f) / 360.0f) * textureWidth;
                        float pixelY = ((90.0f - city.Latitude) / 180.0f) * textureHeight;

                        // Конвертируем цвет
                        var c = city.VisualColor;
                        var textColor = Color.FromRgba((byte)(c.X * 255), (byte)(c.Y * 255), (byte)(c.Z * 255), (byte)(c.W * 255));

                        // Смещение надписи относительно точки города. 
                        // Так как шрифт стал меньше, отступы тоже пропорционально уменьшаем (например, +3 пикселя вбок, -2 вверх)
                        var textPoint = new PointF(pixelX + 3, pixelY - 2);

                        // 1. Рисуем тень (смещение тени тоже уменьшаем до 0.5 пикселя для микрошрифта)
                        ctx.DrawText(city.Name, font, Color.Black, new PointF(textPoint.X + 0.5f, textPoint.Y + 0.5f));

                        // 2. Рисуем основной текст цветом города
                        ctx.DrawText(city.Name, font, textColor, textPoint);
                    }
                });

                // Сохраняем маску в папку Assets
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string projectDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\"));
                string outputPath = Path.Combine(projectDir, "Assets", "cities_text_layer.png");

                image.SaveAsPng(outputPath);
                return outputPath;
            }
        }
    }
}

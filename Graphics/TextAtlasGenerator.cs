using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Geo.Gameplay.Models;
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
        private static int MAX_TRY_ATTEMPS = 30;
        private static float STEP_ATTEMPS = 1.0f;
        private static float CLUSTER_RADIUS_DEGREES = 1.5f;

        public static string GenerateCityTextLayer(WorldSimulation world, int textureWidth, int textureHeight)
        {
            using (var image = new Image<Rgba32>(textureWidth, textureHeight, Color.Transparent))
            {
                var occupiedAreas = new List<RectangleF>();
                var clusters = new Dictionary<CityData, List<CityData>>();

                // --- ЭТАП 1: ГРУППИРОВКА ГОРОДОВ ПО ТАБЛИЧКАМ ---
                foreach (var city in world.Cities)
                {
                    CityData targetLeader = null;
                    foreach (var leader in clusters.Keys)
                    {
                        if (city.Country != leader.Country)
                        {
                            continue; // Пропускаем этого лидера, ищем дальше
                        }

                        float deltaLat = city.Latitude - leader.Latitude;
                        float deltaLon = city.Longitude - leader.Longitude;
                        float distance = MathF.Sqrt(deltaLat * deltaLat + deltaLon * deltaLon);

                        if (distance < CLUSTER_RADIUS_DEGREES)
                        {
                            targetLeader = leader;
                            break;
                        }
                    }

                    if (targetLeader == null) clusters[city] = new List<CityData> { city };
                    else clusters[targetLeader].Add(city);
                }

                // --- ЭТАП 2: РАСЧЕТ И ОТРИСОВКА ТАБЛИЧЕК ---
                image.Mutate(ctx =>
                {
                    foreach (var kvp in clusters)
                    {
                        CityData leader = kvp.Key;
                        List<CityData> groupCities = kvp.Value;

                        float pixelX = ((leader.Longitude + 180.0f) / 360.0f) * textureWidth;
                        float pixelY = ((90.0f - leader.Latitude) / 180.0f) * textureHeight;

                        // Идеальный стартовый отступ для 16K карты
                        float currentTableX = pixelX + 16;
                        float currentTableY = pixelY - (leader.FontSize * 0.5f); // Центрируем по высоте кубика

                        float totalWidth = 0;
                        float totalHeight = 0;

                        foreach (var city in groupCities)
                        {
                            var textOptions = new TextOptions(city.CityFont);
                            FontRectangle size = TextMeasurer.MeasureAdvance(city.Name, textOptions);

                            if (size.Width > totalWidth) totalWidth = size.Width;
                            totalHeight += size.Height + 4.0f;
                        }

                        float paddingX = 6.0f;
                        float paddingY = 4.0f;

                        var tableRect = new RectangleF(
                            currentTableX,
                            currentTableY,
                            totalWidth + (paddingX * 2),
                            totalHeight + (paddingY * 2)
                        );

                        // ====================================================================
                        // 3. УМНЫЙ АЛГОРИТМ УСЛОВНОГО РАСТАЛКИВАНИЯ
                        // ====================================================================
                        // Если город одиночный, мы просто фиксируем его идеальную позицию у кубика
                        if (groupCities.Count > 1)
                        {
                            int attempt = 0;
                            bool isOverlapping = true;

                            while (isOverlapping && attempt < MAX_TRY_ATTEMPS)
                            {
                                isOverlapping = false;

                                // Проверка А: Не накладываемся ли на чужие текстовые плашки
                                foreach (var occupiedRect in occupiedAreas)
                                {
                                    if (tableRect.IntersectsWith(occupiedRect))
                                    {
                                        isOverlapping = true;
                                        break;
                                    }
                                }

                                // Проверка Б: Не накладываемся ли на 3D-тела кубиков этой группы
                                if (!isOverlapping)
                                {
                                    foreach (var city in groupCities)
                                    {
                                        float cityX = ((city.Longitude + 180.0f) / 360.0f) * textureWidth;
                                        float cityY = ((90.0f - city.Latitude) / 180.0f) * textureHeight;

                                        // Ограничиваем радиус кубика 35 пикселями на 16K карте
                                        float cityRadius = 35.0f;
                                        var cityBox = new RectangleF(cityX - cityRadius, cityY - cityRadius, cityRadius * 2, cityRadius * 2);

                                        if (tableRect.IntersectsWith(cityBox))
                                        {
                                            isOverlapping = true;
                                            break;
                                        }
                                    }
                                }

                                // СДВИГ: Вместо прыжков на сотни пикселей, плавно смещаем плашку вниз 
                                // строго на 2 пикселя за шаг. Она аккуратно выползет из-под кубиков и застынет рядом!
                                if (isOverlapping)
                                {
                                    currentTableX += 4.0f;
                                    tableRect = new RectangleF(currentTableX, currentTableY, totalWidth + (paddingX * 2), totalHeight + (paddingY * 2));
                                }
                                else
                                {
                                    break;
                                }

                                attempt++;
                            }
                        }

                        // Добавляем финальную чистую позицию
                        occupiedAreas.Add(tableRect);

                        // --- РИСУЕМ ПОЛУПРОЗРАЧНУЮ ПОДЛОЖКУ ПЛАШКИ ---
                        var backColor = Color.FromRgba(15, 15, 15, 95);
                        ctx.Fill(backColor, tableRect);
                        ctx.Draw(Color.FromRgba(255, 255, 255, 25), 1.5f, tableRect);

                        // --- ПОСТРОЧНАЯ ОТРИСОВКА ТЕКСТА ---
                        float drawY = tableRect.Y + paddingY;

                        foreach (var city in groupCities)
                        {
                            var c = city.VisualColor;
                            var textColor = Color.FromRgba((byte)(c.X * 255), (byte)(c.Y * 255), (byte)(c.Z * 255), (byte)(c.W * 255));

                            var textOptions = new TextOptions(city.CityFont);
                            FontRectangle size = TextMeasurer.MeasureAdvance(city.Name, textOptions);

                            var textPoint = new PointF(tableRect.X + paddingX, drawY);
                            ctx.DrawText(city.Name, city.CityFont, textColor, textPoint);

                            drawY += size.Height + 4.0f;
                        }
                    }
                });

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string projectDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\"));
                string outputPath = Path.Combine(projectDir, "Assets", "cities_text_layer.png");

                image.SaveAsPng(outputPath);
                return outputPath;
            }
        }
    }
}

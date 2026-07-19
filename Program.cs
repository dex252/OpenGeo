using System;
using Vortice.Direct3D11;
using Geo.Core;
using Geo.Graphics; // Добавили пространство имен для работы с классом Texture
using Geo.Math;

class Program
{
    private static GraphicsEngine engine;
    private static ID3D11Buffer planetVertexBuffer;
    private static ID3D11Buffer planetIndexBuffer;
    private static int planetIndexCount;

    // Ссылка на объект нашей текстуры Земли
    private static Texture earthTexture;

    static void Main(string[] args)
    {
        Console.WriteLine("[Система]: Запуск геополитического симулятора...");

        engine = new GraphicsEngine();
        try
        {
            // Разрешение 1024x768, соотношение сторон 4:3. 
            // Обратите внимание: круглая сфера на вытянутом экране может казаться овальной.
            // На следующем этапе мы исправим это с помощью матриц проекции камер.
            engine.Initialize(1024, 768, "Geopolitical Simulator - Earth Core");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Ошибка Инициализации]: {ex.Message}");
            Console.ResetColor();
            return;
        }

        // Компилируем и загружаем шейдеры в видеокарту
        engine.LoadShaders();

        // НОВОЕ: Загружаем нашу картинку Земли. 
        string baseDir = AppDomain.CurrentDomain.BaseDirectory; // Находимся в bin/Debug/net8.0/
        string projectDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, @"..\..\..\")); // Поднимаемся к корню проекта
        string texturePath = System.IO.Path.Combine(projectDir, "Assets", "2k_earth_daymap.jpg"); // Идем прямо в исходную папку Assets

        try
        {
            earthTexture = new Texture(engine.Device, texturePath);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Ошибка загрузки текстуры]: {ex.Message}. Проверьте, что файл лежит в Assets и настроен на копирование!");
            Console.ResetColor();
            engine.Dispose();
            return;
        }

        // Генерируем геометрию сферы на CPU
        var (vertices, indices) = SphereGenerator.Generate(2.0f, 40, 40);
        planetIndexCount = indices.Length;

        // Отправляем буферы в VRAM видеокарты
        (planetVertexBuffer, planetIndexBuffer) = engine.CreateMeshBuffers(vertices, indices);

        // Настройка ограничителя кадров (60 FPS)
        double targetFps = 60.0;
        double targetFrameTimeMs = 1000.0 / targetFps;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        double lastTimeMs = stopwatch.Elapsed.TotalMilliseconds;

        Console.WriteLine($"[Система]: Вход в главный игровой цикл. FPS ограничен на отметке: {targetFps}");

        // Главный цикл симуляции
        while (!engine.ShouldClose())
        {
            // Опрашиваем события окна Silk.NET
            engine.Window.DoEvents();

            double currentTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            double elapsedTimeMs = currentTimeMs - lastTimeMs;

            if (elapsedTimeMs < targetFrameTimeMs)
            {
                int sleepTimeMs = (int)(targetFrameTimeMs - elapsedTimeMs);
                if (sleepTimeMs > 0)
                {
                    System.Threading.Thread.Sleep(sleepTimeMs);
                }
                currentTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            }

            lastTimeMs = currentTimeMs;

            // Рендеринг кадра
            engine.BeginFrame();

            if (planetVertexBuffer != null && planetIndexBuffer != null && earthTexture != null)
            {
                // Связываем вершины, индексы, шейдеры и ТЕКСТУРУ Земли на конвейере GPU
                engine.BindMesh(planetVertexBuffer, planetIndexBuffer, earthTexture.ResourceView);

                // Даем видеокарте приказ отрисовать нашу Землю
                engine.Draw(planetIndexCount);
            }

            engine.EndFrame();
        }

        // КОРРЕКТНОЕ ОСВОБОЖДЕНИЕ ПАМЯТИ
        planetVertexBuffer?.Dispose();
        planetIndexBuffer?.Dispose();
        earthTexture?.Dispose(); // Освобождаем память из-под текстуры
        engine.Dispose();

        Console.WriteLine("[Система]: Работа программы успешно завершена. Все ресурсы очищены.");
    }
}

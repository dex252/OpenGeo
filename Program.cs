using System;
using System.Numerics;
using Vortice.Direct3D11;
using Geo.Core;
using Geo.Graphics;
using Geo.Math;

class Program
{
    private static GraphicsEngine engine;
    private static ID3D11Buffer planetVertexBuffer;
    private static ID3D11Buffer planetIndexBuffer;
    private static int planetIndexCount;
    private static Texture earthTexture;

    // Ссылка на наш новый изолированный класс камеры
    private static OrbitCamera camera;

    static void Main(string[] args)
    {
        Console.WriteLine("[Система]: Запуск геополитического симулятора...");

        engine = new GraphicsEngine();
        try
        {
            engine.Initialize(1024, 768, "Geopolitical Simulator - Earth Core");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Ошибка Инициализации]: {ex.Message}");
            Console.ResetColor();
            return;
        }

        // Инициализируем камеру, передавая ей наше готовое окно Silk.NET
        camera = new OrbitCamera(engine.Window);

        engine.LoadShaders();

        // Загружаем текстуру Земли
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string projectDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, @"..\..\..\"));
        string texturePath = System.IO.Path.Combine(projectDir, "Assets", "8k_earth_daymap.jpg");

        try
        {
            earthTexture = new Texture(engine.Device, texturePath);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Ошибка загрузки текстуры]: {ex.Message}");
            Console.ResetColor();
            engine.Dispose();
            return;
        }

        // Расчет и создание буферов сферы
        var (vertices, indices) = SphereGenerator.Generate(2.0f, 40, 40);
        planetIndexCount = indices.Length;
        (planetVertexBuffer, planetIndexBuffer) = engine.CreateMeshBuffers(vertices, indices);

        // Таймер ограничения кадров
        double targetFps = 60.0;
        double targetFrameTimeMs = 1000.0 / targetFps;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        double lastTimeMs = stopwatch.Elapsed.TotalMilliseconds;

        Console.WriteLine("[Система]: Вход в главный игровой цикл.");

        // Главный цикл симуляции
        while (!engine.ShouldClose())
        {
            engine.Window.DoEvents();

            double currentTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            double elapsedTimeMs = currentTimeMs - lastTimeMs;

            if (elapsedTimeMs < targetFrameTimeMs)
            {
                int sleepTimeMs = (int)(targetFrameTimeMs - elapsedTimeMs);
                if (sleepTimeMs > 0) System.Threading.Thread.Sleep(sleepTimeMs);
                currentTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            }

            lastTimeMs = currentTimeMs;

            // 1. Обновляем состояние камеры (пересчет матриц, если мышь двигалась)
            camera.Update(engine.Window);

            // 2. Рассчитываем итоговую матрицу (Земля неподвижна Identity, камеры берутся из класса)
            Matrix4x4 world = Matrix4x4.Identity;
            Matrix4x4 wvp = world * camera.ViewMatrix * camera.ProjectionMatrix;
            Matrix4x4 wvpTransposed = Matrix4x4.Transpose(wvp);

            // 3. Рендеринг кадра
            engine.BeginFrame();

            if (planetVertexBuffer != null && planetIndexBuffer != null && earthTexture != null)
            {
                engine.UpdateTransform(wvpTransposed);
                engine.BindMesh(planetVertexBuffer, planetIndexBuffer, earthTexture.ResourceView);
                engine.Draw(planetIndexCount);
            }

            engine.EndFrame();
        }

        // Очистка памяти GPU
        planetVertexBuffer?.Dispose();
        planetIndexBuffer?.Dispose();
        earthTexture?.Dispose();
        engine.Dispose();

        Console.WriteLine("[Система]: Работа программы успешно завершена.");
    }
}

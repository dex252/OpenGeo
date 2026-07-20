using Geo.Core;
using Geo.Gameplay.Services;
using Geo.Graphics;
using Geo.Math;
using System;
using System.Numerics;
using Vortice.Direct3D11;

class Program
{
    private static GraphicsEngine engine;
    private static ID3D11Buffer planetVertexBuffer;
    private static ID3D11Buffer planetIndexBuffer;
    private static ID3D11Buffer cameraConstantBuffer;
    private static int planetIndexCount;
    private static Texture earthTexture;

    private static Texture cityTextTexture;

    private static WorldSimulation worldSimulation;

    // Ссылка на наш новый изолированный класс камеры
    private static OrbitCamera camera;

    //private static Vector3 cityPosition;
    //private static ID3D11Buffer cityVertexBuffer;
    //private static ID3D11Buffer cityIndexBuffer;
    //private static int cityIndexCount;

    private static Geo.Graphics.GameWorldRenderer worldRenderer;

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

        // Инициализируем симуляцию мира (передаем радиус сферы 2.0f)
        worldSimulation = new WorldSimulation(2.0f);

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

        // Создаем буферы для Земли (это у вас уже написано)
        var (earthVertices, earthIndices) = SphereGenerator.Generate(2.0f, 40, 40);
        planetIndexCount = earthIndices.Length;
        (planetVertexBuffer, planetIndexBuffer) = engine.CreateMeshBuffers(earthVertices, earthIndices);

        var cameraBufferDesc = new BufferDescription
        {
            Usage = ResourceUsage.Default,
            ByteWidth = 16, // Минимальный размер буфера в DirectX 11 должен быть кратен 16 байтам
            BindFlags = BindFlags.ConstantBuffer
        };
        cameraConstantBuffer = engine.Device.CreateBuffer(cameraBufferDesc);

        // После создания буферов самой Земли:
        worldRenderer = new Geo.Graphics.GameWorldRenderer(engine);

        // Генерируем PNG картинку с текстом под разрешение вашей карты (например, 16384х8192)
        string textLayerPath = Geo.Graphics.TextAtlasGenerator.GenerateCityTextLayer(worldSimulation, 16384, 8192);

        // Загружаем её в видеопамять как вторую текстуру
        cityTextTexture = new Texture(engine.Device, textLayerPath);

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

            // Забираем текущий радиус из нашей камеры
            float currentRadius = camera.CurrentRadius;

            // Создаем массив данных для отправки (DirectX требует кратность 4 флоатам, поэтому дополняем нулями)
            float[] cameraData = new float[] { currentRadius, 0.0f, 0.0f, 0.0f };

            // Обновляем константный буфер на видеокарте
            engine.Device.ImmediateContext.UpdateSubresource(cameraData, cameraConstantBuffer);

            // Обновление симуляции
            worldSimulation.Update(elapsedTimeMs / 1000.0);

            // 2. Рассчитываем итоговую матрицу (Земля неподвижна Identity, камеры берутся из класса)
            Matrix4x4 world = Matrix4x4.Identity;
            Matrix4x4 wvp = world * camera.ViewMatrix * camera.ProjectionMatrix;
            Matrix4x4 wvpTransposed = Matrix4x4.Transpose(wvp);

            // 3. Рендеринг кадра
            engine.BeginFrame();

            // 4. Рисуем космос
            //engine.DrawBackground();

            // 5. Рисуем Землю
            if (planetVertexBuffer != null && planetIndexBuffer != null && earthTexture != null)
            {
                // Отправляем чистую матрицу WVP
                engine.UpdateTransform(wvpTransposed);

                // Метод BindMesh сам включит шейдеры Земли, текстуру и буферы
                engine.BindMesh(planetVertexBuffer, planetIndexBuffer, earthTexture.ResourceView);

                engine.Device.ImmediateContext.PSSetConstantBuffer(1, cameraConstantBuffer);

                // Передаем текстуру текста в слот 1 пиксельного шейдера планеты
                engine.Device.ImmediateContext.PSSetShaderResource(1, cityTextTexture.ResourceView);

                engine.Draw(planetIndexCount);

                //Отрисовка городов
                worldRenderer.Render(worldSimulation, camera);
            }

            engine.EndFrame();
        }

        // Очистка памяти GPU
        planetVertexBuffer?.Dispose();
        planetIndexBuffer?.Dispose();
        cameraConstantBuffer?.Dispose();

        worldRenderer?.Dispose();

        earthTexture?.Dispose();
        cityTextTexture?.Dispose();

        engine.Dispose();

        Console.WriteLine("[Система]: Работа программы успешно завершена.");
    }

}

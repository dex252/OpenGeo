using System;
using System.Numerics; // Добавили системную математику для работы с Matrix4x4 и Vector3
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

    // Ссылка на объект нашей текстуры Земли
    private static Texture earthTexture;

    static void Main(string[] args)
    {
        Console.WriteLine("[Система]: Запуск геополитического симулятора...");

        engine = new GraphicsEngine();
        try
        {
            // Инициализируем окно 1024x768
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

        // Загружаем нашу картинку Земли напрямую из папки проекта
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string projectDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, @"..\..\..\"));
        string texturePath = System.IO.Path.Combine(projectDir, "Assets", "2k_earth_daymap.jpg");

        try
        {
            earthTexture = new Texture(engine.Device, texturePath);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Ошибка загрузки текстуры]: {ex.Message}. Проверьте, что файл лежит в Assets!");
            Console.ResetColor();
            engine.Dispose();
            return;
        }

        // Генерируем геометрию сферы на CPU (Радиус = 2.0, сетка 40х40)
        var (vertices, indices) = SphereGenerator.Generate(2.0f, 40, 40);
        planetIndexCount = indices.Length;

        // Отправляем буферы в VRAM видеокарты
        (planetVertexBuffer, planetIndexBuffer) = engine.CreateMeshBuffers(vertices, indices);

        // Настройка ограничителя кадров (60 FPS)
        double targetFps = 60.0;
        double targetFrameTimeMs = 1000.0 / targetFps;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        double lastTimeMs = stopwatch.Elapsed.TotalMilliseconds;

        // --- МАТЕМАТИКА КАМЕРЫ (НАСТРОЙКА ПЕРЕД ЦИКЛОМ) ---
        // 1. Коэффициент соотношения сторон, чтобы планета была круглой, а не овальной
        float aspectRatio = 1024.0f / 768.0f;

        // 2. Перспективная матрица линзы объектива (Угол обзора 45 градусов, видимость от 0.1 до 100 единиц)
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4.0f, aspectRatio, 0.1f, 100.0f);

        // 3. Матрица положения камеры в космосе. Ставим камеру по оси Z на расстояние -6.0f, направляем взгляд в центр (0,0,0)
        Matrix4x4 view = Matrix4x4.CreateLookAt(new Vector3(0, 0, -6.0f), Vector3.Zero, Vector3.UnitY);

        // Переменная для накопления угла поворота планеты вокруг своей оси
        float rotationAngle = 0.0f;

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

            // --- ОБНОВЛЕНИЕ ТРАНСФОРМАЦИИ КАЖДЫЙ КАДР ---
            // Медленно увеличиваем угол (0.005f за кадр даст плавное и неспешное вращение глобуса)
            rotationAngle += 0.005f;

            // Создаем матрицу мира для планеты (Вращение вокруг вертикальной оси Y)
            Matrix4x4 world = Matrix4x4.CreateRotationY(rotationAngle);

            // Перемножаем все матрицы строго в порядке графического конвейера: Мир * Вид * Проекция
            Matrix4x4 wvp = world * view * projection;

            // Критический шаг для DirectX 11: транспонируем (переворачиваем строки и столбцы) 
            // матрицы перед тем, как отдать их HLSL шейдеру, так как C# и C++ хранят матрицы по-разному
            Matrix4x4 wvpTransposed = Matrix4x4.Transpose(wvp);

            // Рендеринг кадра
            engine.BeginFrame();

            if (planetVertexBuffer != null && planetIndexBuffer != null && earthTexture != null)
            {
                // 1. Отправляем свежую матрицу WVP текущего кадра в константный буфер видеокарты
                engine.UpdateTransform(wvpTransposed);

                // 2. Связываем вершины, индексы, шейдеры и текстуру на конвейере GPU
                engine.BindMesh(planetVertexBuffer, planetIndexBuffer, earthTexture.ResourceView);

                // 3. Командуем отрисовку треугольников сферы
                engine.Draw(planetIndexCount);
            }

            engine.EndFrame();
        }

        // КОРРЕКТНОЕ ОСВОБОЖДЕНИЕ ПАМЯТИ ПРИ ВЫХОДЕ
        planetVertexBuffer?.Dispose();
        planetIndexBuffer?.Dispose();
        earthTexture?.Dispose();
        engine.Dispose();

        Console.WriteLine("[Система]: Работа программы успешно завершена. Все ресурсы очищены.");
    }
}

using System;
using System.Runtime.InteropServices;
using Silk.NET.Windowing;       // Для IWindow и WindowOptions
using Vortice.Direct3D;
using Vortice.Direct3D11;       // Для ID3D11Device и контекста
using Vortice.DXGI;             // Для IDXGISwapChain и RenderTargetView
using Vortice.Mathematics;
using Geo.Graphics;             // Для структуры VertexPositionTexture
using Vortice.D3DCompiler; // Для компиляции HLSL кода в рантайме

namespace Geo.Core
{
    public class GraphicsEngine : IDisposable
    {
        private IWindow _window;
        private ID3D11Device _device;
        private ID3D11DeviceContext _context;
        private IDXGISwapChain _swapChain;
        private ID3D11RenderTargetView _renderTargetView;

        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader _pixelShader;
        private ID3D11InputLayout _inputLayout;

        public IWindow Window => _window;
        public ID3D11Device Device => _device;

        public void Initialize(int width, int height, string title)
        {
            // 1. Настройка и создание окна средствами Silk.NET
            var options = WindowOptions.Default;
            options.Size = new Silk.NET.Maths.Vector2D<int>(width, height);
            options.Title = title;
            // Отключаем встроенную графику (OpenGL), так как будем инициализировать DirectX вручную
            options.API = GraphicsAPI.None;
            options.WindowBorder = WindowBorder.Fixed;

            _window = Silk.NET.Windowing.Window.Create(options);
            _window.Initialize();

            // 2. Инициализация DirectX 11
            // Безопасно забираем нативный HWND окна Windows через встроенный интерфейс
            IntPtr hwnd = IntPtr.Zero;
            if (_window.Native != null && _window.Native.Win32.HasValue)
            {
                hwnd = _window.Native.Win32.Value.Hwnd;
            }

            if (hwnd == IntPtr.Zero)
            {
                throw new Exception("Не удалось получить дескриптор окна HWND для DirectX. Убедитесь, что вы на Windows.");
            }

            var swapChainDesc = new SwapChainDescription
            {
                BufferCount = 1,
                BufferDescription = new ModeDescription((uint)width, (uint)height, new Rational(60u, 1u), Format.R8G8B8A8_UNorm),
                BufferUsage = Usage.RenderTargetOutput,
                OutputWindow = hwnd,
                SampleDescription = new SampleDescription(1, 0),
                Windowed = true,
                SwapEffect = SwapEffect.Discard
            };

            // Создаем устройство (Device), контекст (Context) и буфер вывода (SwapChain)
            D3D11.D3D11CreateDeviceAndSwapChain(
                null,
                DriverType.Hardware,
                DeviceCreationFlags.None,
                new FeatureLevel[] { FeatureLevel.Level_11_0 },
                swapChainDesc,
                out _swapChain,
                out _device,
                out _,
                out _context
            );

            // Создаем RenderTargetView для вывода пикселей в окно
            using (ID3D11Texture2D backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0))
            {
                _renderTargetView = _device.CreateRenderTargetView(backBuffer);
            }

            // Настраиваем область отрисовки (Viewport) под размер окна
            _context.RSSetViewport(new Viewport(0, 0, width, height));
            Console.WriteLine("[Engine]: Системы окна и DirectX 11 успешно запущены.");
        }

        // Проверяет, приказал ли пользователь закрыть окно крестиком
        public bool ShouldClose()
        {
            return _window.IsClosing;
        }

        // Метод создания буферов на основе переданной геометрии сферы
        public (ID3D11Buffer VertexBuffer, ID3D11Buffer IndexBuffer) CreateMeshBuffers(VertexPositionTexture[] vertices, uint[] indices)
        {
            uint vertexStride = (uint)Marshal.SizeOf<VertexPositionTexture>();
            uint vertexByteWidth = vertexStride * (uint)vertices.Length;
            var vDesc = new BufferDescription(vertexByteWidth, BindFlags.VertexBuffer);

            uint indexByteWidth = (uint)(sizeof(uint) * indices.Length);
            var iDesc = new BufferDescription(indexByteWidth, BindFlags.IndexBuffer);

            ID3D11Buffer vBuffer;
            ID3D11Buffer iBuffer;

            unsafe
            {
                fixed (VertexPositionTexture* pVertices = vertices)
                {
                    var vData = new SubresourceData((IntPtr)pVertices);
                    vBuffer = _device.CreateBuffer(vDesc, vData);
                }

                fixed (uint* pIndices = indices)
                {
                    var iData = new SubresourceData((IntPtr)pIndices);
                    iBuffer = _device.CreateBuffer(iDesc, iData);
                }
            }

            return (vBuffer, iBuffer);
        }

        public void BeginFrame()
        {
            var clearColor = new Color4(0.01f, 0.01f, 0.03f, 1.0f); // Космический цвет фона
            _context.ClearRenderTargetView(_renderTargetView, clearColor);
            _context.OMSetRenderTargets(_renderTargetView);
        }

        public void BindMesh(ID3D11Buffer vertexBuffer, ID3D11Buffer indexBuffer, ID3D11ShaderResourceView resourceView)
        {
            uint stride = (uint)Marshal.SizeOf<VertexPositionTexture>();
            uint offset = 0;

            // Привязываем буферы геометрии
            _context.IASetVertexBuffers(0, 1, new[] { vertexBuffer }, new[] { stride }, new[] { offset });
            _context.IASetIndexBuffer(indexBuffer, Vortice.DXGI.Format.R32_UInt, 0);
            _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

            // НОВОЕ: Включаем шейдеры и формат вершин на конвейере видеокарты
            _context.IASetInputLayout(_inputLayout);
            _context.VSSetShader(_vertexShader);
            _context.PSSetShader(_pixelShader);
        }

        public void EndFrame()
        {
            _swapChain.Present(0, PresentFlags.None);//1 - V-Sync
        }

        public void Draw(int indexCount)
        {
            // Отправляет команду видеокарте отрисовать треугольники на основе индексов
            _context.DrawIndexed((uint)indexCount, 0, 0);
        }

        public void LoadShaders()
        {
            // 1. Компилируем Вершинный Шейдер из текстового файла
            // "main" — это имя функции внутри .hlsl файла, "vs_5_0" — версия шейдеров для DX11
            Compiler.CompileFromFile("Shaders/VertexShader.hlsl", "main", "vs_5_0", out Blob vsBlob, out _);
            _vertexShader = _device.CreateVertexShader(vsBlob.AsBytes());

            // 2. Компилируем Пиксельный Шейдер из текстового файла
            Compiler.CompileFromFile("Shaders/PixelShader.hlsl", "main", "ps_5_0", out Blob psBlob, out _);
            _pixelShader = _device.CreatePixelShader(psBlob.AsBytes());

            // 3. Описываем формат вершины (Input Layout) для видеокарты
            // Мы должны строго сопоставить поля нашей C# структуры VertexPositionTexture с HLSL
            InputElementDescription[] inputElements = new InputElementDescription[]
            {
                // Позиция: 3 числа float (R32G32B32_Float), семантика "POSITION"
                new InputElementDescription("POSITION", 0, Vortice.DXGI.Format.R32G32B32_Float, 0, 0),
                // Текстурные координаты: 2 числа float (R32G32_Float), семантика "TEXCOORD"
                // AlignedByteOffset = 12, так как перед ней идут 3 float по 4 байта каждый (3 * 4 = 12)
                new InputElementDescription("TEXCOORD", 0, Vortice.DXGI.Format.R32G32_Float, 12, 0)
            };

            // Создаем Input Layout, связывая описание формата со скомпилированным вершинным шейдером
            _inputLayout = _device.CreateInputLayout(inputElements, vsBlob.AsBytes());

            // Освобождаем временные буферы компиляции из оперативной памяти
            vsBlob.Dispose();
            psBlob.Dispose();

            Console.WriteLine("[Engine]: Шейдеры успешно скомпилированы и загружены в GPU.");
        }

        public void Dispose()
        {
            // НОВОЕ: Очищаем шейдеры
            _inputLayout?.Dispose();
            _vertexShader?.Dispose();
            _pixelShader?.Dispose();

            _renderTargetView?.Dispose();
            _swapChain?.Dispose();
            _context?.Dispose();
            _device?.Dispose();
            _window?.Close();
            _window?.Dispose();
            Console.WriteLine("[Engine]: Ресурсы графики очищены.");
        }
    }
}

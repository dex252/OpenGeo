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
        private ID3D11RasterizerState _rasterizerState;
        private ID3D11DepthStencilState _depthStencilState;
        private ID3D11DepthStencilView _depthStencilView;
        
        private ID3D11SamplerState _samplerState;

        private IWindow _window;
        private ID3D11Device _device;
        private ID3D11DeviceContext _context;
        private IDXGISwapChain _swapChain;
        private ID3D11RenderTargetView _renderTargetView;

        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader _pixelShader;

        private ID3D11InputLayout _inputLayout;

        private ID3D11Buffer _constantBuffer;

        public IWindow Window => _window;
        public ID3D11Device Device => _device;

        public void Initialize(int width, int height, string title)
        {
            // 1. Настройка и создание окна средствами Silk.NET
            var options = WindowOptions.Default;
            options.Size = new Silk.NET.Maths.Vector2D<int>(width, height);
            options.Title = title;
            options.API = GraphicsAPI.None;

            // МЕНЯЕМ ЭТИ СТРОКИ:
            options.WindowBorder = WindowBorder.Resizable; // Разрешаем растягивать окно мышкой
            options.WindowState = WindowState.Normal;      // По умолчанию окно открывается в обычном режиме

            // Если вы хотите запустить игру СРАЗУ на весь экран (Fullscreen), раскомментируйте строку ниже:
            // options.WindowState = WindowState.Fullscreen; 

            _window = Silk.NET.Windowing.Window.Create(options);
            _window.Initialize();

            //Связываем событие изменения размера окна Silk.NET с нашим методом DirectX
            _window.Resize += (newSize) =>
            {
                this.Resize(newSize.X, newSize.Y);
            };

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

            var cbDesc = new BufferDescription(64, BindFlags.ConstantBuffer); // 64 байта под матрицу 4х4
            _constantBuffer = _device.CreateBuffer(cbDesc);

            // 1. Настройка двухсторонней отрисовки
            var rasterizerDesc = new RasterizerDescription
            {
                CullMode = CullMode.None, // Отключаем отсечение, чтобы видеть переднюю стенку
                FillMode = FillMode.Solid
            };
            _rasterizerState = _device.CreateRasterizerState(rasterizerDesc);

            // 2. Включаем тест глубины
            var depthStencilDesc = new DepthStencilDescription
            {
                DepthEnable = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthFunc = ComparisonFunction.Less
            };
            _depthStencilState = _device.CreateDepthStencilState(depthStencilDesc);

            // 3. Создаем сам буфер глубины под размер окна
            var depthTexDesc = new Texture2DDescription
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D24_UNorm_S8_UInt, // Формат для теста глубины
                SampleDescription = new SampleDescription(1, 0), // Без сглаживания (должно совпадать со SwapChain)
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CPUAccessFlags = CpuAccessFlags.None,
                MiscFlags = ResourceOptionFlags.None
            };

            // Создаем сэмплер для плавной фильтрации текстуры Земли
            var samplerDesc = new SamplerDescription
            {
                Filter = Filter.MinMagMipLinear, // Линейное сглаживание пикселей
                AddressU = TextureAddressMode.Wrap, // Повторять текстуру по горизонтали (чтобы глобус бесшовно сходился)
                AddressV = TextureAddressMode.Clamp, // Зажимать на полюсах, чтобы не было швов
                AddressW = TextureAddressMode.Wrap
            };

            _samplerState = _device.CreateSamplerState(samplerDesc);

            using (ID3D11Texture2D depthTexture = _device.CreateTexture2D(depthTexDesc))
            {
                _depthStencilView = _device.CreateDepthStencilView(depthTexture);
            }

            Console.WriteLine("[Engine]: Системы окна и DirectX 11 успешно запущены.");
        }

        // Проверяет, приказал ли пользователь закрыть окно крестиком
        public bool ShouldClose()
        {
            return _window.IsClosing;
        }

        public void UpdateTransform(System.Numerics.Matrix4x4 matrix)
        {
            // Обновляем данные буфера на видеокарте
            _context.UpdateSubresource(matrix, _constantBuffer);
            // Привязываем буфер к Вертексному Шейдеру в слот 0
            _context.VSSetConstantBuffer(0, _constantBuffer);
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
            var clearColor = new Color4(0.01f, 0.01f, 0.03f, 1.0f);
            _context.ClearRenderTargetView(_renderTargetView, clearColor);

            // Очищаем Z-буфер перед рисованием нового кадра
            _context.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            // Связываем вместе буфер цвета и буфер глубины
            _context.OMSetRenderTargets(_renderTargetView, _depthStencilView);
        }

        public void BindMesh(ID3D11Buffer vertexBuffer, ID3D11Buffer indexBuffer, ID3D11ShaderResourceView textureView)
        {
            uint stride = (uint)System.Runtime.InteropServices.Marshal.SizeOf<VertexPositionTexture>();
            uint offset = 0;

            // 1. Привязываем буферы геометрии сферы к конвейеру
            _context.IASetVertexBuffers(0, 1, new[] { vertexBuffer }, new[] { stride }, new[] { offset });
            _context.IASetIndexBuffer(indexBuffer, Vortice.DXGI.Format.R32_UInt, 0);
            _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

            // 2. Включаем формат вершин и шейдеры самой планеты Земля
            _context.IASetInputLayout(_inputLayout);
            _context.VSSetShader(_vertexShader);
            _context.PSSetShader(_pixelShader);

            // 3. Передаем текстуру Земли и сэмплер сглаживания в Пиксельный шейдер (слоты t0 и s0)
            _context.PSSetShaderResource(0, textureView);
            _context.PSSetSampler(0, _samplerState);

            // 4. Активируем двухстороннюю отрисовку треугольников сферы
            _context.RSSetState(_rasterizerState);

            // 5. КРИТИЧЕСКИ ВАЖНО ДЛЯ ХОРОШЕЙ АРХИТЕКТУРЫ:
            // Связываем буфер цвета И буфер глубины вместе для отрисовки 3D-сферы
            _context.OMSetRenderTargets(_renderTargetView, _depthStencilView);

            // Включаем стандартный 3D тест глубины (чтобы передние материки перекрывали задние)
            _context.OMSetDepthStencilState(_depthStencilState, 0);
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
            // Земля
            Compiler.CompileFromFile("Shaders/VertexShader.hlsl", "main", "vs_5_0", out Blob vsBlob, out _);
            _vertexShader = _device.CreateVertexShader(vsBlob.AsBytes());
            Compiler.CompileFromFile("Shaders/PixelShader.hlsl", "main", "ps_5_0", out Blob psBlob, out _);
            _pixelShader = _device.CreatePixelShader(psBlob.AsBytes());

            // Layout привязываем к вершинному шейдеру Земли, так как у космоса нет Layout (мы рисуем его без буфера вершин)
            InputElementDescription[] inputElements = new InputElementDescription[]
            {
                new InputElementDescription("POSITION", 0, Vortice.DXGI.Format.R32G32B32_Float, 0, 0),
                new InputElementDescription("TEXCOORD", 0, Vortice.DXGI.Format.R32G32_Float, 12, 0)
            };
            _inputLayout = _device.CreateInputLayout(inputElements, vsBlob.AsBytes());

            // Освобождаем временные буферы компиляции из оперативной памяти
            vsBlob.Dispose();
            psBlob.Dispose();

            Console.WriteLine("[Engine]: Шейдеры успешно скомпилированы и загружены в GPU.");
        }

        public void Resize(int newWidth, int height)
        {
            // Если размеры нулевые (например, игру свернули в трей), ничего не делаем
            if (newWidth == 0 || height == 0) return;

            // 1. Обязательно освобождаем старые привязки и буферы (иначе DirectX выдаст ошибку)
            _context.OMSetRenderTargets((ID3D11RenderTargetView)null, null);
            _renderTargetView?.Dispose();
            _depthStencilView?.Dispose();

            // 2. Приказываем SwapChain изменить размер внутреннего буфера под новое окно
            _swapChain.ResizeBuffers(1, (uint)newWidth, (uint)height, Format.R8G8B8A8_UNorm, SwapChainFlags.None).CheckError();

            // 3. Пересоздаем RenderTargetView для нового размера
            using (ID3D11Texture2D backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0))
            {
                _renderTargetView = _device.CreateRenderTargetView(backBuffer);
            }

            // 4. Пересоздаем Буфер Глубины (Z-Buffer) под новое разрешение
            var depthTexDesc = new Texture2DDescription
            {
                Width = (uint)newWidth,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D24_UNorm_S8_UInt,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil
            };

            using (ID3D11Texture2D depthTexture = _device.CreateTexture2D(depthTexDesc))
            {
                _depthStencilView = _device.CreateDepthStencilView(depthTexture);
            }

            // 5. Обновляем область отрисовки (Viewport)
            _context.RSSetViewport(new Viewport(0, 0, newWidth, height));

            Console.WriteLine($"[Engine]: Разрешение успешно изменено на {newWidth}x{height}");
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
            _constantBuffer?.Dispose();

            _rasterizerState?.Dispose();
            _depthStencilState?.Dispose();
            _depthStencilView?.Dispose();
            _samplerState?.Dispose();

            Console.WriteLine("[Engine]: Ресурсы графики очищены.");
        }
    }
}

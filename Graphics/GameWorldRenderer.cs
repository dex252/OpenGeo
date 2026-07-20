using System;
using System.IO;
using System.Numerics;
using Vortice.Direct3D11;
using Vortice.D3DCompiler; // Нужно для компиляции шейдеров налету
using Geo.Core;
using Geo.Gameplay.Services;
using Vortice.Direct3D;

namespace Geo.Graphics
{
    public class GameWorldRenderer : IDisposable
    {
        private readonly GraphicsEngine _engine;

        // Буферы геометрии куба
        private ID3D11Buffer _cityVertexBuffer;
        private ID3D11Buffer _cityIndexBuffer;
        private int _cityIndexCount;

        // Компоненты для маркеров городов
        private ID3D11VertexShader _markerVertexShader;
        private ID3D11PixelShader _markerPixelShader;
        private ID3D11Buffer _colorConstantBuffer; // Буфер для передачи цвета в GPU

        public GameWorldRenderer(GraphicsEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));

            InitializeMarkers();
            LoadMarkerShaders();
        }

        private void InitializeMarkers()
        {
            // Создаем геометрию куба базового размера 1.0f через Фабрику
            var (cubeVertices, cubeIndices) = MeshFactory.CreateCube(1.0f);
            _cityIndexCount = cubeIndices.Length;
            (_cityVertexBuffer, _cityIndexBuffer) = _engine.CreateMeshBuffers(cubeVertices, cubeIndices);

            // Создаем константный буфер для цвета
            var bufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                ByteWidth = (uint)System.Runtime.InteropServices.Marshal.SizeOf<ColorBufferData>(),
                BindFlags = BindFlags.ConstantBuffer
            };
            _colorConstantBuffer = _engine.Device.CreateBuffer(bufferDesc);
        }

        private void LoadMarkerShaders()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Собираем пути к новым шейдерам городов
            string vsPath = Path.Combine(baseDir, "Shaders", "Cities", "MarkerVertexShader.hlsl");
            string psPath = Path.Combine(baseDir, "Shaders", "Cities", "MarkerPixelShader.hlsl");

            // Компилируем и создаем Вершинный Шейдер маркеров
            Compiler.CompileFromFile(vsPath, "main", "vs_5_0", out Blob vsBlob, out _);
            _markerVertexShader = _engine.Device.CreateVertexShader(vsBlob.AsBytes());
            vsBlob.Dispose();

            // Компилируем и создаем Пиксельный Шейдер маркеров
            Compiler.CompileFromFile(psPath, "main", "ps_5_0", out Blob psBlob, out _);
            _markerPixelShader = _engine.Device.CreatePixelShader(psBlob.AsBytes());
            psBlob.Dispose();
        }

        public void Render(WorldSimulation world, OrbitCamera camera)
        {
            if (world == null || camera == null) return;
            if (_cityVertexBuffer == null || _cityIndexBuffer == null) return;

            var context = _engine.Device.ImmediateContext;

            // 1. Подготовка контекста: привязываем меш и шейдеры маркеров
            _engine.BindMesh(_cityVertexBuffer, _cityIndexBuffer, null);
            context.VSSetShader(_markerVertexShader);
            context.PSSetShader(_markerPixelShader);
            context.PSSetConstantBuffer(1, _colorConstantBuffer);

            // 2. Отрисовываем каждый город ровно в ОДИН вызов Draw
            foreach (var city in world.Cities)
            {
                // Считаем масштаб и матрицы (Масштаб 0.01f)
                float baseMultiplier = 0.01f;
                float finalScale = baseMultiplier * city.VisualScale;

                Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(finalScale);
                Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(city.CartesianPosition);
                Matrix4x4 cityWorld = scaleMatrix * translationMatrix;
                Matrix4x4 cityWvp = cityWorld * camera.ViewMatrix * camera.ProjectionMatrix;
                _engine.UpdateTransform(Matrix4x4.Transpose(cityWvp));

                // Обновляем цвет в буфере
                var colorData = new ColorBufferData { MarkerColor = city.VisualColor };
                context.UpdateSubresource(colorData, _colorConstantBuffer);

                // Видеокарта сама покрасит ребра в черный за этот единственный вызов:
                _engine.Draw(_cityIndexCount);
            }
        }


        public void Dispose()
        {
            _cityVertexBuffer?.Dispose();
            _cityIndexBuffer?.Dispose();
            _markerVertexShader?.Dispose();
            _markerPixelShader?.Dispose();
            _colorConstantBuffer?.Dispose();
        }
    }
}

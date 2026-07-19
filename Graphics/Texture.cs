using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Geo.Graphics
{
    public class Texture : IDisposable
    {
        private ID3D11ShaderResourceView _resourceView;

        public ID3D11ShaderResourceView ResourceView => _resourceView;

        public Texture(ID3D11Device device, string filePath)
        {
            // 1. Загружаем картинку любого разрешения через ImageSharp
            using (Image<Rgba32> image = Image.Load<Rgba32>(filePath))
            {
                int width = image.Width;
                int height = image.Height;

                // Извлекаем сырые байты пикселей в один сплошной массив
                byte[] pixelBytes = new byte[width * height * 4];
                image.CopyPixelDataTo(pixelBytes);

                // 2. Описываем текстуру для DirectX 11
                var textureDesc = new Texture2DDescription
                {
                    Width = (uint)width,
                    Height = (uint)height,
                    MipLevels = 1, // Пока без мип-маппинга для простоты
                    ArraySize = 1,
                    Format = Format.R8G8B8A8_UNorm, // Стандартный RGBA 32-бит формат
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.ShaderResource, // Говорим, что её будет читать шейдер
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = ResourceOptionFlags.None
                };

                // Обертываем указатель на массив пикселей
                unsafe
                {
                    fixed (byte* pPixels = pixelBytes)
                    {
                        var subresourceData = new SubresourceData((IntPtr)pPixels, (uint)width * 4, 0);

                        // Создаем нативную 2D текстуру на видеокарте
                        using (ID3D11Texture2D texture2D = device.CreateTexture2D(textureDesc, new[] { subresourceData }))
                        {
                            // Создаем Shader Resource View (интерфейс, через который шейдер видит текстуру)
                            _resourceView = device.CreateShaderResourceView(texture2D);
                        }
                    }
                }
            }
            Console.WriteLine($"[GPU]: Текстура {filePath} успешно загружена на видеокарту.");
        }

        public void Dispose()
        {
            _resourceView?.Dispose();
        }
    }
}

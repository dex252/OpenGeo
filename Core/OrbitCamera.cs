using System;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Geo.Core
{
    public class OrbitCamera
    {
        // --- КОНСТАНТЫ НАСТРОЙКИ КАМЕРЫ ---
        private const float BASE_SENSITIVITY = 0.005f; // Базовая чувствительность мыши
        private const float ZOOM_SENSITIVITY = 0.1f;   // Скорость зума при прокрутке колесика

        // Ваша новая настроенная дистанция
        private const float MIN_RADIUS = 2.5f;         // Максимальное приближение к Земле
        private const float MAX_RADIUS = 5.0f;         // Максимальное отдаление в космос

        // Задаем минимальную скорость вблизи как 5% от базовой (было 15%). Это сделает вращение вблизи «ювелирным».
        private const float MIN_ROTATION_FACTOR = 0.05f;
        // Задаем максимальную скорость на отдалении как 100% от базовой.
        private const float MAX_ROTATION_FACTOR = 1.0f;

        // --- СОСТОЯНИЕ КАМЕРЫ ---
        private float _yaw = 0.0f;       // Вращение по горизонтали
        private float _pitch = 0.0f;     // Вращение по вертикали
        private float _radius = 4.0f;    // Текущая дистанция (стартуем посредине диапазона)

        private bool _isLeftMouseDown = false;
        private Vector2 _lastMousePos = Vector2.Zero;

        public Matrix4x4 ViewMatrix { get; private set; }
        public Matrix4x4 ProjectionMatrix { get; private set; }
        public float CurrentRadius => _radius;


        public OrbitCamera(IWindow window)
        {
            float aspectRatio = (float)window.Size.X / window.Size.Y;
            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4.0f, aspectRatio, 0.1f, 100.0f);

            IInputContext input = window.CreateInput();
            if (input.Mice.Count > 0)
            {
                IMouse mouse = input.Mice[0];

                mouse.MouseDown += (m, button) =>
                {
                    if (button == MouseButton.Left)
                    {
                        _isLeftMouseDown = true;
                        _lastMousePos = new Vector2(m.Position.X, m.Position.Y);
                    }
                };

                mouse.MouseUp += (m, button) =>
                {
                    if (button == MouseButton.Left)
                    {
                        _isLeftMouseDown = false;
                    }
                };

                mouse.MouseMove += (m, pos) =>
                {
                    if (_isLeftMouseDown)
                    {
                        float deltaX = pos.X - _lastMousePos.X;
                        float deltaY = pos.Y - _lastMousePos.Y;

                        // Вычисляем чистый процент нахождения камеры между MIN и MAX дистанциями (от 0.0 до 1.0)
                        float zoomPercent = (_radius - MIN_RADIUS) / (MAX_RADIUS - MIN_RADIUS);

                        // ДИНАМИЧЕСКИЙ РАСЧЕТ: Масштабируем скорость вращения строго между нашими константами-лимитами
                        float zoomFactor = zoomPercent * (MAX_ROTATION_FACTOR - MIN_ROTATION_FACTOR) + MIN_ROTATION_FACTOR;

                        // На всякий случай страхуем математику от выхода за границы
                        zoomFactor = (float)global::System.Math.Clamp(zoomFactor, MIN_ROTATION_FACTOR, MAX_ROTATION_FACTOR);

                        // Применяем итоговую скорость к осям
                        _yaw += deltaX * BASE_SENSITIVITY * zoomFactor;
                        _pitch += deltaY * BASE_SENSITIVITY * zoomFactor;

                        // Ограничение, чтобы не перевернуть камеру на полюсах
                        float maxPitch = MathF.PI / 2.0f - 0.01f;
                        _pitch = (float)global::System.Math.Clamp(_pitch, -maxPitch, maxPitch);

                        _lastMousePos = new Vector2(pos.X, pos.Y);
                    }
                };

                mouse.Scroll += (m, wheel) =>
                {
                    // Используем константу скорости зума
                    _radius -= wheel.Y * ZOOM_SENSITIVITY;

                    // Используем константы ограничений расстояния
                    _radius = (float)global::System.Math.Clamp(_radius, MIN_RADIUS, MAX_RADIUS);
                };
            }

            UpdateMatrices();
        }

        public void Update(IWindow window)
        {
            float aspectRatio = (float)window.Size.X / window.Size.Y;
            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4.0f, aspectRatio, 0.1f, 100.0f);

            UpdateMatrices();
        }

        private void UpdateMatrices()
        {
            float camX = _radius * MathF.Cos(_pitch) * MathF.Sin(_yaw);
            float camY = _radius * MathF.Sin(_pitch);
            float camZ = -_radius * MathF.Cos(_pitch) * MathF.Cos(_yaw);

            Vector3 cameraPosition = new Vector3(camX, camY, camZ);
            ViewMatrix = Matrix4x4.CreateLookAt(cameraPosition, Vector3.Zero, Vector3.UnitY);
        }
    }
}

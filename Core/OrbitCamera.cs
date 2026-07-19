using System;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Geo.Core
{
    public class OrbitCamera
    {
        // Настройки и состояние камеры
        private float _yaw = 0.0f;       // Вращение по горизонтали
        private float _pitch = 0.0f;     // Вращение по вертикали
        private float _radius = 6.0f;    // Дистанция до Земли (зум)

        private bool _isLeftMouseDown = false;
        private Vector2 _lastMousePos = Vector2.Zero;

        // Итоговые матрицы, которые мы будем забирать для DirectX
        public Matrix4x4 ViewMatrix { get; private set; }
        public Matrix4x4 ProjectionMatrix { get; private set; }

        public OrbitCamera(IWindow window)
        {
            // Расчет начальной матрицы проекции (линзы объектива)
            float aspectRatio = (float)window.Size.X / window.Size.Y;
            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4.0f, aspectRatio, 0.1f, 100.0f);

            // Получаем контекст ввода из окна Silk.NET
            IInputContext input = window.CreateInput();
            if (input.Mice.Count > 0)
            {
                IMouse mouse = input.Mice[0];

                // Подписка на нажатия кнопок мыши
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

                // Подписка на движение мыши
                mouse.MouseMove += (m, pos) =>
                {
                    if (_isLeftMouseDown)
                    {
                        float deltaX = pos.X - _lastMousePos.X;
                        float deltaY = pos.Y - _lastMousePos.Y;

                        _yaw += deltaX * 0.005f; // Чувствительность горизонтали
                        _pitch += deltaY * 0.005f; // Чувствительность вертикали

                        // Ограничение на полюсах
                        float maxPitch = MathF.PI / 2.0f - 0.01f;
                        _pitch = (float)global::System.Math.Clamp(_pitch, -maxPitch, maxPitch);

                        _lastMousePos = new Vector2(pos.X, pos.Y);
                    }
                };

                // Подписка на колесико мыши
                mouse.Scroll += (m, wheel) =>
                {
                    _radius -= wheel.Y * 0.5f;
                    // Ограничиваем дистанцию: от 2.5f (приближение) до 5.0f (отдаление)
                    _radius = (float)global::System.Math.Clamp(_radius, 2.5f, 5.0f); // Границы приближения
                };
            }

            // Считаем начальную матрицу вида
            UpdateMatrices();
        }

        public void Update()
        {
            // Метод для вызова каждый кадр (пересчитывает положение камеры при движении)
            UpdateMatrices();
        }

        private void UpdateMatrices()
        {
            // Переводим сферические углы в 3D координаты X, Y, Z
            float camX = _radius * MathF.Cos(_pitch) * MathF.Sin(_yaw);
            float camY = _radius * MathF.Sin(_pitch);
            float camZ = -_radius * MathF.Cos(_pitch) * MathF.Cos(_yaw);

            Vector3 cameraPosition = new Vector3(camX, camY, camZ);

            // Создаем матрицу взгляда View. Камера в cameraPosition, смотрит в (0,0,0)
            ViewMatrix = Matrix4x4.CreateLookAt(cameraPosition, Vector3.Zero, Vector3.UnitY);
        }
    }
}

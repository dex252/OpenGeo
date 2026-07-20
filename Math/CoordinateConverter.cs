using System;
using System.Numerics;

namespace Geo.Math
{
    public static class CoordinateConverter
    {
        /// <summary>
        /// Переводит географические координаты (в градусах) в 3D-координаты на сфере.
        /// </summary>
        /// <param name="latitude">Широта от -90 (Южный полюс) до 90 (Северный полюс)</param>
        /// <param name="longitude">Долгота от -180 (Запад) до 180 (Восток)</param>
        /// <param name="radius">Радиус вашей 3D сферы (в вашем случае 2.0f)</param>
        public static Vector3 GeoToCartesian(float latitude, float longitude, float radius)
        {
            // 1. Широта (Latitude) в радианы
            float latRad = MathF.PI * latitude / 180.0f;
            float phi = (MathF.PI / 2.0f) - latRad;

            // 2. Долгота (Longitude) в радианы с учетом инверсии шейдера и сдвига текстуры на 180 градусов
            float lonRad = MathF.PI * (-longitude) / 180.0f + MathF.PI;
            float theta = lonRad;

            // 3. Сборка по формуле SphereGenerator
            float x = radius * MathF.Sin(phi) * MathF.Cos(theta);
            float y = radius * MathF.Cos(phi);
            float z = radius * MathF.Sin(phi) * MathF.Sin(theta);

            return new Vector3(x, y, z);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Geo.Gameplay.Models
{
    public class CityData
    {
        // Уникальный идентификатор города (пригодится для дипломатии и торговли)
        public Guid Id { get; } = Guid.NewGuid();

        public string Name { get; set; }

        // Географические координаты (для логики симуляции)
        public float Latitude { get; set; }
        public float Longitude { get; set; }

        // Кэшированная 3D-позиция в пространстве (для графического движка)
        public Vector3 CartesianPosition { get; set; }

        // Параметры геополитического симулятора
        public int Population { get; set; }
        public float Stability { get; set; } = 1.0f; // от 0.0 до 1.0

        public CityData(string name, float latitude, float longitude, float sphereRadius, int population)
        {
            Name = name;
            Latitude = latitude;
            Longitude = longitude;
            Population = population;

            // Сразу рассчитываем 3D-точку на сфере, используя наш проверенный конвертер
            // Радиус чуть-чуть увеличиваем (например, на +0.02f), чтобы маркер гарантированно не ушел под землю
            CartesianPosition = Math.CoordinateConverter.GeoToCartesian(latitude, longitude, sphereRadius + 0.005f);
        }

        /// <summary>
        /// Возвращает графический масштаб маркера в зависимости от населения города
        /// </summary>
        public float VisualScale => Population switch
        {
            >= 25_000_000   =>      1.0f,    // Мега-агломерации (Токио, Шанхай)
            >= 12_500_000   =>      0.85f,    // Сверхкрупные столицы (Москва)
            >= 7_500_000    =>      0.7f,    // Крупные мировые центры (Нью-Йорк)
            >= 5_000_000    =>      0.6f,    // Большие мегаполисы (Сидней, Санкт-Петербург)
            >= 2_500_000    =>      0.5f,   // Развитые региональные центры
            >= 1_000_000    =>      0.4f,    // Миллионники (Новосибирск)
            >= 100_000      =>      0.3f,   // Средние города (Владивосток, Калининград)
            _               =>      0.2f   // Малые города и поселения (Суздаль, Верхоянск)
        };

        public Vector4 VisualColor => Population switch
        {
            >= 25_000_000   =>      new Vector4(1.0f, 0.0f, 1.0f, 1.0f),  // Маджента (Пурпурный)
            >= 12_500_000   =>      new Vector4(1.0f, 0.0f, 0.0f, 1.0f),  // Красный
            >= 7_500_000    =>      new Vector4(1.0f, 0.35f, 0.0f, 1.0f), // Ярко-оранжевый
            >= 5_000_000    =>      new Vector4(1.0f, 0.6f, 0.0f, 1.0f),  // Оранжевый
            >= 2_500_000    =>      new Vector4(1.0f, 1.0f, 0.0f, 1.0f),  // Желтый
            >= 1_000_000    =>      new Vector4(0.0f, 0.8f, 1.0f, 1.0f),  // Голубой
            >= 100_000      =>      new Vector4(0.0f, 1.0f, 0.0f, 1.0f),  // Зеленый (Владивосток попадет сюда)
            _               =>      new Vector4(1.0f, 1.0f, 1.0f, 1.0f)   // Белый (Крошечные поселения)
        };
    }
}

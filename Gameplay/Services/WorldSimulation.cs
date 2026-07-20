using Geo.Gameplay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geo.Gameplay.Services
{
    public class WorldSimulation
    {
        // Наш потокобезопасный (в будущем) список всех городов на планете
        private readonly List<CityData> _cities = new List<CityData>();

        // Открытое свойство только для чтения для графического движка
        public IReadOnlyList<CityData> Cities => _cities;

        private readonly float _planetRadius;

        public WorldSimulation(float planetRadius)
        {
            _planetRadius = planetRadius;
            InitializeWorld();
        }

        /// <summary>
        /// Первоначальное наполнение мира городами
        /// </summary>
        private void InitializeWorld()
        {
            WorldDataSeeder.SeedCities(_cities, _planetRadius);
        }

        private void AddCity(string name, float lat, float lon, int population, string country)
        {
            var city = new CityData(name, lat, lon, _planetRadius, population, country);
            _cities.Add(city);
            Console.WriteLine($"[Симуляция]: Создан город: {name} ({lat}, {lon}) -> 3D: {city.CartesianPosition}");
        }

        /// <summary>
        /// Метод для будущего игрового такта (Update экономики, населения и т.д.)
        /// Будет вызываться каждый кадр или каждую игровую секунду
        /// </summary>
        public void Update(double deltaTime)
        {
            // Здесь будет крутиться вся геополитика, пока оставляем заглушку
        }
    }
}

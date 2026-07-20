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
            Console.WriteLine("[Симуляция]: Наполнение мира городами для теста 8-цветной палитры...");

            // 1. КАТЕГОРИЯ: Маджента (>= 25 млн) — Сверх-мегаполисы
            AddCity("Токио", 35.6762f, 139.6503f, 37_000_000);
            AddCity("Шанхай", 31.2304f, 121.4737f, 26_000_000);

            // 2. КАТЕГОРИЯ: Красный (12.5 - 25 млн) — Мировые столицы-гиганты
            AddCity("Москва", 55.7558f, 37.6173f, 13_000_000);
            AddCity("Стамбул", 41.0082f, 28.9784f, 15_500_000);

            // 3. КАТЕГОРИЯ: Ярко-оранжевый (7.5 - 12.5 млн) — Крупные мегаполисы
            AddCity("Нью-Йорк", 40.7128f, -74.0060f, 8_500_000);
            AddCity("Лондон", 51.5074f, -0.1278f, 9_000_000);

            // 4. КАТЕГОРИЯ: Оранжевый (5 - 7.5 млн) — Большие столицы и центры
            AddCity("Санкт-Петербург", 59.9343f, 30.3351f, 5_600_000);
            AddCity("Сидней", -33.8688f, 151.2093f, 5_300_000);

            // 5. КАТЕГОРИЯ: Желтый (2.5 - 5 млн) — Развитые крупные города
            AddCity("Новокузнецк (Агломерация)", 53.7596f, 87.1216f, 2_600_000); // Сделаем условный миллионник-агломерацию для теста
            AddCity("Рим", 41.9028f, 12.4964f, 2_800_000);

            // 6. КАТЕГОРИЯ: Голубой (1 - 2.5 млн) — Города-миллионники
            AddCity("Новосибирск", 55.0084f, 82.9357f, 1_600_000);
            AddCity("Екатеринбург", 56.8389f, 60.6057f, 1_500_000);

            // 7. КАТЕГОРИЯ: Зеленый (100 тыс - 1 млн) — Крупные региональные центры
            AddCity("Владивосток", 43.1198f, 131.8869f, 600_000);
            AddCity("Хабаровск", 48.4802f, 135.0719f, 610_000);
            AddCity("Калининград", 54.7104f, 20.4522f, 490_000);

            // 8. КАТЕГОРИЯ: Белый (< 100 тыс) — Малые города и поселения
            AddCity("Суздаль", 56.4193f, 40.4487f, 9_000);
            AddCity("Верхоянск", 67.5447f, 133.3951f, 1_000);
        }

        private void AddCity(string name, float lat, float lon, int population)
        {
            var city = new CityData(name, lat, lon, _planetRadius, population);
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

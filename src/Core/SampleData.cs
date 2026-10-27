using Core.Domain;

namespace Core;

/// <summary>
/// Демонстраційні дані (20 товарів, 4 одиниці виміру: шт, м, уп, компл) для сховища в пам'яті
/// і для звітів тижня 7, а також генератор великого набору для вимірювань.
/// </summary>
public static class SampleData
{
    // Кожен виклик будує нові екземпляри: сутності змінювані, спільні об'єкти між сховищами не потрібні.
    public static IReadOnlyList<Product> Products()
    {
        Product tablet = Product.Create("P-007", "Планшет попередньої моделі", 150.00m, 3, "розпродаж залишків");
        tablet.ChangeStatus(ProductStatus.Discontinued);

        Product printer = Product.Create("P-009", "Принтер лазерний", 210.00m, 5, "очікує сервісу");
        printer.ChangeStatus(ProductStatus.Suspended);

        return new List<Product>
        {
            // ── штучні товари (шт) ──
            Product.Create("P-001", "Ноутбук Dell Latitude", 1250.50m, 8, "Акція"),
            Product.Create("P-002", "Мишка бездротова", 25.99m, 120),
            Product.Create("P-003", "Клавіатура механічна", 140.00m, 45),
            Product.Create("P-004", "Монітор 27 дюймів", 300.00m, 15),
            Product.Create("P-005", "Кабель HDMI 2 м", 10.50m, 300),
            Product.Create("P-006", "Навушники", 85.99m, 60),
            tablet,
            Product.Create("P-008", "SSD 1TB", 120.00m, 30),
            printer,

            // ── погонні метри (м) ──
            Product.Create("P-010", "Кабель UTP cat6", 1.20m, 1000, "бухта 305 м", unit: "м"),
            Product.Create("P-011", "Кабель живлення ПВС 3×1.5", 2.80m, 250, unit: "м"),
            Product.Create("P-012", "Гофротруба 20 мм", 0.90m, 150, unit: "м"),
            Product.Create("P-013", "Кабель-канал 25×16", 3.50m, 9, unit: "м"),

            // ── упаковки (уп) ──
            Product.Create("P-014", "Флешка 64GB (уп. 10 шт)", 155.00m, 20, "USB 3.0", unit: "уп"),
            Product.Create("P-015", "Картридж чорний (уп. 2 шт)", 96.00m, 0, unit: "уп"),
            // Навмисно інший регістр: GroupBy з StringComparer.OrdinalIgnoreCase зведе "УП" і "уп" в одну групу.
            Product.Create("P-016", "Стяжки кабельні 200 мм (уп. 100 шт)", 35.00m, 50, unit: "УП"),
            Product.Create("P-017", "Батарейки AA (уп. 4 шт)", 48.00m, 64, unit: "уп"),

            // ── комплекти (компл) ──
            Product.Create("P-018", "Комплект клавіатура + миша", 45.00m, 12, unit: "компл"),
            Product.Create("P-019", "Набір інструментів монтажника", 650.00m, 7, unit: "компл"),
            Product.Create("P-020", "Кріплення для монітора VESA", 380.00m, 25, unit: "компл")
        };
    }

    private static readonly string[] GeneratedUnits = ["шт", "м", "уп", "компл"];

    /// <summary>
    /// Додаткове завдання 4: детермінований великий набір для вимірювань Stopwatch.
    /// Однаковий seed → однакові дані за кожного запуску, тож час різних варіантів можна порівнювати.
    /// </summary>
    public static IReadOnlyList<Product> Generate(int count, int seed = 42)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var random = new Random(seed);
        var result = new List<Product>(count);
        for (int i = 1; i <= count; i++)
        {
            result.Add(Product.Create(
                $"G-{i:D6}",
                $"Згенерований товар {i}",
                random.Next(1, 100_000) / 100m,
                random.Next(0, 1_000),
                unit: GeneratedUnits[random.Next(GeneratedUnits.Length)]));
        }
        return result;
    }
}

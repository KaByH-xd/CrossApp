using Core.Domain;

namespace Core;

/// <summary>Демонстраційні дані (18 товарів) для сховища в пам'яті, а також для звітів тижня 7.</summary>
public static class SampleData
{
    // Кожен виклик будує нові екземпляри: сутності змінювані, спільні об'єкти між сховищами не потрібні.
    public static IReadOnlyList<Product> Products()
    {
        Product printer = Product.Create("P-013", "Принтер лазерний", 210.00m, 5, "очікує сервісу");
        printer.ChangeStatus(ProductStatus.Suspended);

        Product tablet = Product.Create("P-017", "Планшет попередньої моделі", 150.00m, 3, "розпродаж залишків");
        tablet.ChangeStatus(ProductStatus.Discontinued);

        return new List<Product>
        {
            Product.Create("P-001", "Ноутбук Dell Latitude", 1250.50m, 8, "Акція"),
            Product.Create("P-002", "Мишка бездротова", 25.99m, 120),
            Product.Create("P-003", "Клавіатура механічна", 140.00m, 45),
            Product.Create("P-004", "Монітор 27 дюймів", 300.00m, 15),
            Product.Create("P-005", "Кабель HDMI 2 м", 10.50m, 300),
            Product.Create("P-006", "Навушники", 85.99m, 60),
            Product.Create("P-007", "Килимок для миші", 15.00m, 0),
            Product.Create("P-008", "Веб-камера Full HD", 55.00m, 22),
            Product.Create("P-009", "USB-хаб на 7 портів", 32.40m, 40),
            Product.Create("P-010", "Флешка 64GB", 15.50m, 200, "USB 3.0"),
            Product.Create("P-011", "SSD 1TB", 120.00m, 30),
            Product.Create("P-012", "Роутер Wi-Fi 6", 89.90m, 18),
            printer,
            Product.Create("P-014", "Картридж чорний", 48.00m, 0),
            Product.Create("P-015", "Док-станція", 99.99m, 12),
            Product.Create("P-016", "Блок живлення 65W", 27.50m, 75),
            tablet,
            Product.Create("P-018", "Кабель UTP cat6 (за метр)", 1.20m, 1000, "бухта 305 м"),
            Product.Create("P-019", "Блок живлення", 1500.00m, 0)
        };
    }
}

using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

/// <summary>
/// Сценарій лабораторної 6 (події, Action/Func, лямбди), перенесений з Program.cs без змін у логіці.
/// Запуск: dotnet run --project src/Cli -f net10.0 -- lab6 [файл.csv|файл.json]
/// </summary>
internal static class EventsDemo
{
    public static void Run(CatalogService service, ICatalogStore store, string[] args)
    {
        // ═════════════ Підписка на події сервісу (тут, у Cli, а не в Core) ═════════════
        string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.AppendAllText(logPath,
            $"# запуск {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}, сховище {store.GetType().Name}{Environment.NewLine}");
        Console.WriteLine($"Журнал подій: {logPath}");

        // Підписник 1 — локальний метод: має стале ім'я, тому його можна і підписати (+=), і відписати (-=).
        void OnCatalogChanged(object? sender, CatalogChangedEventArgs e)
        {
            string line = ChangeLine.Format(e);
            Console.WriteLine(line);
            try
            {
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Збій журналу не повинен ламати бізнес-операцію, що вже відбулася.
                Console.WriteLine($"  [журнал] не вдалося записати: {ex.Message}");
            }
        }

        service.Changed += OnCatalogChanged;

        // Підписник 2 — лямбда із замиканням: захоплює локальну змінну changes і збільшує саме її.
        int changes = 0;
        service.Changed += (_, _) => changes++;

        // ───────────── Сценарій 1: успішні операції — рівно одна подія на кожну ─────────────
        Console.WriteLine();
        Console.WriteLine("=== Сценарій 1: успішні операції (один рядок події на операцію) ===");
        Product created = service.Add("Кабель UTP cat6", 28.50m, 50, "бухта", unit: "м");
        service.Receive(created.Id, 25);
        service.Issue(created.Id, 10);
        service.ChangePrice(created.Id, 31.00m);
        Product adapter = service.Add("Перехідник USB-C", 9.90m, 0); // нульовий залишок — його можна видалити
        service.Remove(adapter.Id);
        Console.WriteLine($"Подій отримано: {changes}");

        // Порядок обробників: третій, тимчасовий підписник бачить, що перші два вже відпрацювали.
        // Лямбду збережено у змінній — лише так її можна потім зняти через -=.
        EventHandler<CatalogChangedEventArgs> orderProbe = (sender, e) =>
            Console.WriteLine($"  ↳ 3-й обробник: рядок події вже надруковано (1-й), лічильник уже = {changes} (2-й); " +
                              $"sender — {sender?.GetType().Name}");
        service.Changed += orderProbe;
        service.Receive(created.Id, 5);
        service.Changed -= orderProbe;

        // ───────────── Сценарій 2: відмови — виняток є, рядка події немає ─────────────
        Console.WriteLine();
        Console.WriteLine("=== Сценарій 2: відмови (обробники підписані, але подій бути не повинно) ===");
        int changesBeforeFailures = changes;
        ConsoleScenario.TryDo("прихід з від'ємною кількістю", () => service.Receive(created.Id, -5));
        ConsoleScenario.TryDo("прихід неіснуючого id", () => service.Receive("NO-SUCH-ID", 5));
        ConsoleScenario.TryDo("видача більша за залишок", () => service.Issue(created.Id, 1000));
        ConsoleScenario.TryDo("нульова кількість приходу", () => service.Receive(created.Id, 0));
        ConsoleScenario.TryDo("від'ємна ціна", () => service.ChangePrice(created.Id, -1m));
        ConsoleScenario.TryDo("порожня назва нового товару", () => service.Add(" ", 1m, 1));
        ConsoleScenario.TryDo("порожня одиниця виміру", () => service.Add("Товар", 1m, 1, unit: " "));
        ConsoleScenario.TryDo("дубль id у сховищі", () => store.Add(Product.Create(created.Id, "Копія", 1m)));
        ConsoleScenario.TryDo("видалення товару із залишком", () => service.Remove(created.Id));
        ConsoleScenario.TryDo("сервіс без сховища", () => new CatalogService(null!));
        Console.WriteLine($"Подій під час відмов: {changes - changesBeforeFailures} (очікується 0)");
        Console.WriteLine($"Стан після усіх відмов: {service.Find(created.Id)}");

        // ───────────── Сценарій 3: відписка — операція виконується, повідомлення немає ─────────────
        Console.WriteLine();
        Console.WriteLine("=== Сценарій 3: відписка ===");
        Console.WriteLine($"Подій отримано: {changes}");
        service.Changed -= OnCatalogChanged;
        service.Receive(created.Id, 5); // тиха операція: рядка події немає ні в консолі, ні в app.log
        Console.WriteLine($"Після відписки дані змінились: {service.Find(created.Id)}");
        Console.WriteLine($"Подій отримано: {changes}"); // +1 лише від лямбди-лічильника

        // Нова лямбда з тим самим текстом — інший екземпляр делегата, тому ця відписка нічого не знімає.
        service.Changed -= (_, _) => changes++;
        service.Issue(created.Id, 5);
        Console.WriteLine($"Після «-= нова лямбда»: подій {changes} — лічильник досі підписаний");

        // ───────────── Сценарій 4: Func замість події — потрібне значення у відповідь ─────────────
        Console.WriteLine();
        Console.WriteLine("=== Сценарій 4: FindAll(p => p.Quantity < 10) ===");
        IReadOnlyList<Product> lowStock = service.FindAll(p => p.Quantity < 10);
        Console.WriteLine($"Товарів із залишком < 10: {lowStock.Count}");
        foreach (Product p in lowStock)
            Console.WriteLine($"  {p}");
        Console.WriteLine($"Усього записів: {service.All().Count}; ціна > 100: {service.FindAll(p => p.Price > 100m).Count} шт.");

        // ───────────── Додаткове 1: INotificationSink через конструктор (ручний DI) ─────────────
        Console.WriteLine();
        Console.WriteLine("=== Додаткове 1: одержувачі INotificationSink, передані в конструктор ===");
        string sinkLogPath = Path.Combine(AppContext.BaseDirectory, "logs", "sink.log");
        var notifyingService = new CatalogService(store, new ConsoleSink(), new FileSink(sinkLogPath));
        int changesBeforeSinks = changes;
        notifyingService.Receive(created.Id, 3);
        notifyingService.Issue(created.Id, 2);
        Console.WriteLine($"FileSink пише у: {sinkLogPath}");
        Console.WriteLine($"Подій на service.Changed за цей час: {changes - changesBeforeSinks} " +
                          "(інший екземпляр сервісу — його одержувачі задані конструктором, а не через +=)");

        // ───────────── Додаткове 2: Action<string> як параметр методу імпорту ─────────────
        Console.WriteLine();
        Console.WriteLine("=== Додаткове 2: звіт про пропущені рядки імпорту через Action<string> ===");
        string? importPath = args.FirstOrDefault(a =>
                a.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
                a.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            ?? FindUp(Path.Combine("data", "sample.csv"));

        if (importPath is null || !File.Exists(importPath))
        {
            Console.WriteLine($"Файл імпорту не знайдено ({importPath ?? "data/sample.csv"}) — розділ пропущено.");
        }
        else
        {
            int skipped = 0;
            Action<string> reportSkipped = reason =>
            {
                skipped++; // замикання: лічильник живе в Cli, імпортер про нього не знає
                Console.WriteLine($"  пропущено: {reason}");
            };

            MultiImportResult raw = importPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? ProductJsonImporter.Load(importPath, reportSkipped)
                : ProductCsvImporter.Load(importPath, reportSkipped);
            DomainImportResult imported = DomainConverter.ToDomain(raw, reportSkipped);
            Console.WriteLine($"Імпорт {Path.GetFileName(importPath)}: товарів {imported.Products.Count}, " +
                              $"складів {imported.Warehouses.Count}, пропущено {skipped}");
        }

        if (store is CachingCatalogStore cache)
            Console.WriteLine($"Кеш: влучань {cache.Hits}, промахів {cache.Misses}");
    }

    // Шукає відносний шлях у поточному каталозі та вище від каталогу збірки (запуск і з кореня, і з IDE).
    private static string? FindUp(string relativePath)
    {
        if (File.Exists(relativePath))
            return Path.GetFullPath(relativePath);

        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            string candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }
}

using Core.Events;
using Core.Reports;
using Core.Services;

/// <summary>
/// Додаткове завдання 1: друга колекція — рух товарів, зібраний з подій CatalogService.Changed (тиждень 6),
/// і звіти Join / GroupJoin над «товари × переміщення».
/// </summary>
internal static class MovementsDemo
{
    public static void Run(CatalogService service)
    {
        var movements = new List<StockMovement>();
        EventHandler<CatalogChangedEventArgs> record = (_, e) => movements.Add(StockMovement.From(e));
        service.Changed += record;
        try
        {
            // Id із SampleData; якщо джерело інше (--file, --empty), невдалі операції просто пропускаються.
            Try("прихід картриджів P-015 +10", () => service.Receive("P-015", 10));
            Try("видача бухти UTP P-010 −305", () => service.Issue("P-010", 305));
            Try("видача мишок P-002 −5", () => service.Issue("P-002", 5));
            Try("прихід ноутбуків P-001 +4", () => service.Receive("P-001", 4));
            Try("видача ноутбуків P-001 −2", () => service.Issue("P-001", 2));
            Try("прихід на призупинений принтер P-009", () => service.Receive("P-009", 1)); // інваріант → руху немає

            // Тимчасовий товар: додано і видалено → 2 записи руху для товару, якого в каталозі вже немає.
            var temp = service.Add("Тестер мережевий", 420.00m, 0);
            service.Remove(temp.Id);
        }
        finally
        {
            service.Changed -= record; // підписка лише на час демо
        }

        IReadOnlyList<MovementLine> journal = ReportService.MovementJournal(service.All(), movements);
        ReportPrinter.Title($"Join: журнал руху (записів руху {movements.Count}, у звіті {journal.Count})");
        ReportPrinter.Print(journal);
        Console.WriteLine($"  Join — внутрішнє з'єднання: {movements.Count - journal.Count} записи(ів) руху " +
                          "видаленого товару не мають пари в каталозі і до звіту не потрапили.");

        ReportPrinter.Title("GroupJoin: рух по кожному товару (і без переміщень теж)");
        ReportPrinter.Print(ReportService.MovementsByProduct(service.All(), movements));
    }

    private static void Try(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"  виконано: {title}");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Console.WriteLine($"  пропущено: {title} — {ex.Message}");
        }
    }
}

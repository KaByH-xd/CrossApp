using Core.Reports;

/// <summary>
/// Лише вивід звітів у консоль із вирівнюванням колонок. Жодних обчислень: усі запити — у Core.ReportService.
/// </summary>
internal static class ReportPrinter
{
    private const int NameWidth = 36;

    public static void Title(string text)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {text} ===");
    }

    public static void Print(IReadOnlyList<StockLine> lines)
    {
        if (lines.Count == 0)
        {
            Console.WriteLine("  (немає записів)");
            return;
        }
        Console.WriteLine($"  {"Id",-9} {"Назва",-NameWidth} {"Залишок",8}  Од.");
        foreach (StockLine l in lines)
            Console.WriteLine($"  {l.Id,-9} {Fit(l.Name),-NameWidth} {l.Quantity,8}  {l.Unit}");
    }

    public static void Print(IReadOnlyList<GroupSummary> groups)
    {
        if (groups.Count == 0)
        {
            Console.WriteLine("  (немає записів)");
            return;
        }
        foreach (GroupSummary g in groups)
            Console.WriteLine($"  {g.Key,-6} позицій: {g.Count,3}   сумарно: {g.TotalQuantity,6}");
    }

    public static void Print((int Items, int Total, double Average, int Max) t) =>
        Console.WriteLine($"  позицій {t.Items}, всього {t.Total}, середнє {t.Average:F2}, макс {t.Max}");

    public static void Print(IReadOnlyList<StockValueLine> lines)
    {
        if (lines.Count == 0)
        {
            Console.WriteLine("  (немає записів)");
            return;
        }
        Console.WriteLine($"  {"Id",-9} {"Назва",-NameWidth} {"Залишок",8} {"Од.",-5} {"Ціна",10} {"Вартість",12}");
        foreach (StockValueLine l in lines)
            Console.WriteLine($"  {l.Id,-9} {Fit(l.Name),-NameWidth} {l.Quantity,8} {l.Unit,-5} {l.Price,10:F2} {l.Value,12:F2}");
    }

    public static void Print(IReadOnlyList<MovementLine> lines)
    {
        if (lines.Count == 0)
        {
            Console.WriteLine("  (немає записів)");
            return;
        }
        Console.WriteLine($"  {"Id",-9} {"Назва",-NameWidth} {"Операція",-9} {"Зміна",7}  Од.");
        foreach (MovementLine l in lines)
            Console.WriteLine($"  {l.Id,-9} {Fit(l.Name),-NameWidth} {l.Kind,-9} {l.Delta,7:+0;-0}  {l.Unit}");
    }

    public static void Print(IReadOnlyList<MovementSummary> lines)
    {
        if (lines.Count == 0)
        {
            Console.WriteLine("  (немає записів)");
            return;
        }
        Console.WriteLine($"  {"Id",-9} {"Назва",-NameWidth} {"Рухів",5} {"Прихід",7} {"Видача",7} {"Залишок",8}  Од.");
        foreach (MovementSummary s in lines)
            Console.WriteLine($"  {s.Id,-9} {Fit(s.Name),-NameWidth} {s.Moves,5} {s.Incoming,7} {s.Outgoing,7} {s.QuantityNow,8}  {s.Unit}");
    }

    // Довгі назви обрізаються, щоб не ламати колонки.
    private static string Fit(string text) =>
        text.Length <= NameWidth ? text : text[..(NameWidth - 1)] + "…";
}

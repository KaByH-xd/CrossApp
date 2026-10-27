using Core.Events;

namespace Core.Reports;

/// <summary>
/// Переміщення (рух) товару — друга колекція для Join/GroupJoin (додаткове завдання 1).
/// Записи будуються з подій CatalogService.Changed (тиждень 6): підписник у Cli додає їх у список.
/// </summary>
public sealed record StockMovement(string ProductId, CatalogChangeKind Kind, int Delta, int QuantityAfter, DateTimeOffset At)
{
    public static StockMovement From(CatalogChangedEventArgs change)
    {
        ArgumentNullException.ThrowIfNull(change);
        return new StockMovement(change.Id, change.Kind, change.Delta, change.QuantityAfter, change.At);
    }
}

/// <summary>Рядок журналу руху (результат Join): переміщення + назва й одиниця товару.</summary>
public sealed record MovementLine(string Id, string Name, CatalogChangeKind Kind, int Delta, string Unit);

/// <summary>Зведення руху по товару (результат GroupJoin): є і для товарів без жодного переміщення.</summary>
public sealed record MovementSummary(
    string Id, string Name, int Moves, int Incoming, int Outgoing, int QuantityNow, string Unit);

namespace Core.Reports;

/// <summary>Підсумок однієї групи: ключ (одиниця виміру), кількість позицій і сумарний залишок.</summary>
public sealed record GroupSummary(string Key, int Count, int TotalQuantity);

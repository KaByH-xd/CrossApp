namespace Core.Reports;

/// <summary>
/// Рядок звіту про залишок. DTO, а не Product: контракт звіту (Api — тиждень 10, Blazor — тиждень 12)
/// не залежить від внутрішньої будови сутності; record незмінний і порівнюється за значенням (тести тижня 8).
/// </summary>
public sealed record StockLine(string Id, string Name, int Quantity, string Unit);

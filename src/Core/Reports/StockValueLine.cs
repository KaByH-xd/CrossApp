namespace Core.Reports;

/// <summary>Рядок звіту «вартість залишку»: Value = Price × Quantity.</summary>
public sealed record StockValueLine(string Id, string Name, int Quantity, string Unit, decimal Price, decimal Value);

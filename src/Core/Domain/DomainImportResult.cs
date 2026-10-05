namespace Core.Domain;

/// <summary>Результат перетворення DTO у сутності: коректні об'єкти плюс опис відхилених.</summary>
public sealed record DomainImportResult(
    IReadOnlyList<Product> Products,
    IReadOnlyList<Warehouse> Warehouses,
    IReadOnlyList<string> Errors);

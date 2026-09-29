namespace Core.Dto;

public sealed record MultiImportResult(
    IReadOnlyList<ProductDto> Products,
    IReadOnlyList<WarehouseDto> Warehouses,
    IReadOnlyList<string> Errors);
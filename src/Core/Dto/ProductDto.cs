namespace Core.Dto;

public record ProductDto(
    string Id,
    string Name,
    decimal Price,
    string? Note = null,
    int Quantity = 0,
    string Status = "Active");

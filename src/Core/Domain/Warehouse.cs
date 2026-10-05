using Core.Dto;

namespace Core.Domain;

/// <summary>Склад. Ідентифікатор і назва незмінні, адресу можна змінити лише через <see cref="Relocate"/>.</summary>
public sealed class Warehouse
{
    public string Id { get; }
    public string Name { get; }
    public string Location { get; private set; }

    private Warehouse(string id, string name, string location)
    {
        Id = id;
        Name = name;
        Location = location;
    }

    public static Warehouse Create(string id, string name, string location)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор складу не може бути порожнім", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Назва складу {id.Trim()} не може бути порожньою", nameof(name));
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException($"Розташування складу {id.Trim()} не може бути порожнім", nameof(location));

        return new Warehouse(id.Trim().ToUpperInvariant(), name.Trim(), location.Trim());
    }

    public void Relocate(string newLocation)
    {
        if (string.IsNullOrWhiteSpace(newLocation))
            throw new ArgumentException("Нове розташування не може бути порожнім", nameof(newLocation));
        if (string.Equals(Location, newLocation.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Склад {Id} уже розташований за адресою '{Location}'");

        Location = newLocation.Trim();
    }

    public WarehouseDto ToDto() => new(Id, Name, Location);

    public static Warehouse FromDto(WarehouseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return Create(dto.Id, dto.Name, dto.Location);
    }

    public override string ToString() => $"{Id} {Name} ({Location})";
}

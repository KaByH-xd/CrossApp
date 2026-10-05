using System.Globalization;
using Core.Dto;

namespace Core.Domain;

/// <summary>
/// Товар на складі. Сутність із поведінкою: стан змінюється лише методами,
/// які перевіряють інваріанти. Створити товар можна тільки через <see cref="Create"/>.
/// </summary>
public sealed class Product
{
    private int _quantity;

    public string Id { get; }
    public string Name { get; }
    public string? Note { get; }
    public decimal Price { get; private set; }
    public int Quantity => _quantity;
    public ProductStatus Status { get; private set; }

    private Product(string id, string name, decimal price, int quantity, string? note)
    {
        Id = id;
        Name = name;
        Price = price;
        _quantity = quantity;
        Note = note;
        Status = ProductStatus.Active;
    }

    // Єдиний спосіб створити товар: усі перевірки виконуються ДО створення об'єкта.
    public static Product Create(string id, string name, decimal price, int quantity = 0, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор товару не може бути порожнім", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Назва товару {id.Trim()} не може бути порожньою", nameof(name));
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), price,
                $"Ціна товару {id.Trim()} не може бути від'ємною");
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity,
                $"Початковий залишок товару {id.Trim()} не може бути від'ємним");

        return new Product(
            id.Trim().ToUpperInvariant(),
            name.Trim(),
            price,
            quantity,
            string.IsNullOrWhiteSpace(note) ? null : note.Trim());
    }

    public void RegisterArrival(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount,
                "Кількість приходу має бути більшою за нуль");
        EnsureActive("прихід");
        if (amount > int.MaxValue - _quantity)
            throw new InvalidOperationException(
                $"Не можна прийняти {amount}: залишок {Id} = {_quantity} перевищить допустимий максимум {int.MaxValue}");

        _quantity += amount;
    }

    public void Issue(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount,
                "Кількість видачі має бути більшою за нуль");
        EnsureActive("видачу");
        if (amount > _quantity)
            throw new InvalidOperationException(
                $"Не можна видати {amount}: залишок {Id} = {_quantity}");

        _quantity -= amount;
    }

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(newPrice), newPrice,
                "Нова ціна не може бути від'ємною");
        EnsureActive("зміну ціни");

        Price = newPrice;
    }

    public void ChangeStatus(ProductStatus target)
    {
        if (!Enum.IsDefined(target))
            throw new ArgumentOutOfRangeException(nameof(target), target,
                "Невідомий статус товару");
        if (!IsTransitionAllowed(Status, target))
            throw new InvalidOperationException(
                $"Товар {Id}: перехід зі статусу {Status} у {target} неможливий");

        Status = target;
    }

    // Допустимі переходи між статусами; Discontinued — кінцевий стан.
    private static bool IsTransitionAllowed(ProductStatus from, ProductStatus to) => (from, to) switch
    {
        (ProductStatus.Active, ProductStatus.Suspended) => true,
        (ProductStatus.Active, ProductStatus.Discontinued) => true,
        (ProductStatus.Suspended, ProductStatus.Active) => true,
        (ProductStatus.Suspended, ProductStatus.Discontinued) => true,
        _ => false
    };

    private void EnsureActive(string operation)
    {
        if (Status != ProductStatus.Active)
            throw new InvalidOperationException(
                $"Товар {Id} має статус {Status}: {operation} неможлива");
    }

    // Мапінг у формат даних (DTO) і назад — його використає сховище п'ятого тижня.
    public ProductDto ToDto() => new(Id, Name, Price, Note, Quantity, Status.ToString());

    // Відновлення проходить ТІ САМІ перевірки, що й створення: через Create і ChangeStatus.
    public static Product FromDto(ProductDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!Enum.TryParse(dto.Status, ignoreCase: true, out ProductStatus status) || !Enum.IsDefined(status))
            throw new ArgumentException(
                $"Товар {dto.Id}: невідомий статус '{dto.Status}'", nameof(dto));

        Product product = Create(dto.Id, dto.Name, dto.Price, dto.Quantity, dto.Note);
        if (status != ProductStatus.Active)
            product.ChangeStatus(status);
        return product;
    }

    public override string ToString() =>
        $"{Id} {Name} — {Price.ToString("F2", CultureInfo.InvariantCulture)}, залишок {Quantity} [{Status}]";
}

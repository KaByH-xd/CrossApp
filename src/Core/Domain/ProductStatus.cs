namespace Core.Domain;

/// <summary>Життєвий цикл товару на складі.</summary>
public enum ProductStatus
{
    /// <summary>Товар в обігу: дозволені прихід, видача і зміна ціни.</summary>
    Active,

    /// <summary>Тимчасово призупинений: жодні операції з залишком не дозволені.</summary>
    Suspended,

    /// <summary>Знятий з обігу: кінцевий стан, повернення в обіг неможливе.</summary>
    Discontinued
}

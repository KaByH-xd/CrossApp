using Core.Events;

namespace Core.Abstractions;

/// <summary>
/// Додаткове завдання 1: одержувач сповіщень, якого сервіс отримує через конструктор (ручний DI).
/// Core знає лише контракт; куди саме піде повідомлення (екран, файл, мережа), вирішує точка входу.
/// На відміну від події, набір одержувачів фіксується при створенні сервісу і не змінюється через += / -=.
/// </summary>
public interface INotificationSink
{
    /// <summary>Викликається після кожної успішної зміни каталогу.</summary>
    void Notify(CatalogChangedEventArgs change);
}

using Core.Abstractions;
using Core.Events;

/// <summary>Додаткове завдання 1: одержувач сповіщень, що друкує їх на екран.</summary>
internal sealed class ConsoleSink : INotificationSink
{
    public void Notify(CatalogChangedEventArgs change) =>
        Console.WriteLine($"[ConsoleSink] {ChangeLine.Format(change)}");
}

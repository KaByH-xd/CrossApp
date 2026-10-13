/// <summary>Спільний допоміжний код сценаріїв: один обробник винятків для всіх випадків.</summary>
internal static class ConsoleScenario
{
    // На екран іде лише тип і Message, без stack trace.
    public static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
        }
    }
}

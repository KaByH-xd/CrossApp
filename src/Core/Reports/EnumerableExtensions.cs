namespace Core.Reports;

/// <summary>Додаткове завдання 2: власний метод-розширення для топ-N, спільний для кількох звітів.</summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Перші <paramref name="n"/> елементів за спаданням <paramref name="key"/>; за однакового ключа —
    /// за зростанням унікального <paramref name="tieBreaker"/> (Ordinal), тож порядок однаковий за кожного запуску.
    /// Повертає відкладений запит: виконується лише під час перебирання (ToList у викликача).
    /// Аргументи перевіряються одразу, бо метод не є ітератором (немає yield).
    /// </summary>
    public static IEnumerable<T> TopBy<T, TKey>(
        this IEnumerable<T> source, Func<T, TKey> key, int n, Func<T, string> tieBreaker)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(tieBreaker);
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        return source
            .OrderByDescending(key)
            .ThenBy(tieBreaker, StringComparer.Ordinal)
            .Take(n);
    }
}

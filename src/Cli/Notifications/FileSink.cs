using Core.Abstractions;
using Core.Events;

/// <summary>
/// Додаткове завдання 1: одержувач сповіщень, що дописує їх у текстовий файл.
/// Помилка запису не має ламати бізнес-операцію, тому вона перехоплюється тут, у одержувачі.
/// </summary>
internal sealed class FileSink : INotificationSink
{
    private readonly string _path;

    public FileSink(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    }

    public void Notify(CatalogChangedEventArgs change)
    {
        try
        {
            File.AppendAllText(_path, ChangeLine.Format(change) + Environment.NewLine);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"[FileSink] не вдалося записати у {_path}: {ex.Message}");
        }
    }
}

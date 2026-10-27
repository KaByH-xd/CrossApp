using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Storage;

/// <summary>
/// Варіант composition root «через фабрику» (додаткове завдання 3): вибір реалізації винесено
/// з Program.cs. Живе в Cli, тож межа абстракції не порушується, але конкретні класи
/// вже не зосереджені в одному файлі. Порівняння — у README.
/// </summary>
internal static class StoreFactory
{
    public static ICatalogStore Create(string[] args, string dataPath)
    {
        ICatalogStore store = args.Contains("--file")
            ? new FileCatalogStore(dataPath)
            : new InMemoryCatalogStore(args.Contains("--empty") ? Array.Empty<Product>() : SampleData.Products());

        return args.Contains("--cache") ? new CachingCatalogStore(store) : store;
    }
}

using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

/// <summary>
/// Файлове сховище: кеш у пам'яті, дозавантаження при першому зверненні, запис на диск після
/// кожної зміни. На диск іде список ProductDto (формат), а не сама сутність.
/// </summary>
public sealed class FileCatalogStore(string path) : ICatalogStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<string, Product> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded) return;

        if (File.Exists(_path))
        {
            List<ProductDto> dtos;
            try
            {
                dtos = JsonSerializer.Deserialize<List<ProductDto>>(File.ReadAllText(_path), Options) ?? [];
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Файл {_path} має неправильний формат (виправте або видаліть його): {ex.Message}", ex);
            }

            foreach (ProductDto dto in dtos)
            {
                Product product = Product.FromDto(dto); // ті самі інваріанти, що й під час Create
                if (!_cache.TryAdd(product.Id, product))
                    throw new InvalidOperationException(
                        $"Файл {_path} містить дублікат id={product.Id}.");
            }
        }
        _loaded = true;
    }

    private void Flush()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path,
            JsonSerializer.Serialize(_cache.Values.Select(p => p.ToDto()).ToList(), Options));
    }

    public IReadOnlyList<Product> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList().AsReadOnly();
    }

    public Product? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Запис з id={item.Id} уже існує.");
        _cache.Add(item.Id, item);
        Flush();
    }

    public void Update(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (!_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Немає запису з id={item.Id}.");
        _cache[item.Id] = item;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id)) return false;
        Flush();
        return true;
    }
}

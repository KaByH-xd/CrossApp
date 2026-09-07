using System.Runtime.InteropServices;
using System.Text.Json;

// Перевіряємо, чи передано аргумент --json
bool useJson = args.Contains("--json");

if (useJson)
{
    // Створюємо анонімний об'єкт для JSON
    var info = new
    {
        Title = "CrossApp практикум з крос-платформного програмування",
        Student = "Прізвище Ім'я, група", // Вставте ваші дані
        OSDescription = RuntimeInformation.OSDescription,
        OSEnvironment = Environment.OSVersion.ToString(),
        ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
        DotNetVersion = Environment.Version.ToString(),
        Runtime = RuntimeInformation.FrameworkDescription,
        AppDirectory = AppContext.BaseDirectory,
        CurrentDirectory = Environment.CurrentDirectory,
        Domain = "Замовлення" // Вставте ваш домен
    };

    // Серіалізуємо та виводимо JSON-рядок
    Console.WriteLine(JsonSerializer.Serialize(info));
}
else
{
    // Стандартний вивід таблицею
    Console.WriteLine("CrossApp практикум з крос-платформного програмування");
    Console.WriteLine("Студент: Михалюк Володимир, група ФЕІ-32 "); // Вставте ваші дані
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"OC (OSDescription): {RuntimeInformation.OSDescription}");
    Console.WriteLine($"OC (Environment) : {Environment.OSVersion}");
    Console.WriteLine($"Архітектура процесу: {RuntimeInformation.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR): {Environment.Version}");
    Console.WriteLine($"Runtime : {RuntimeInformation.FrameworkDescription}");
    Console.WriteLine($"Каталог застосунку: {AppContext.BaseDirectory}");
    Console.WriteLine($"Поточний каталог : {Environment.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine("Предметна область: Замовлення"); // Вставте ваш домен
}
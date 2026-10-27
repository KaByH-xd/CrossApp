# CrossApp
Наскрізний проєкт з крос-платформного програмування.

## Предметна область
**Склад.** Сутності: Product (товар), StockBatch (партія), Warehouse (склад), Movement (переміщення)[cite: 1, 3].
Призначення: облік залишків товарів по партіях[cite: 1, 3].

## Структура solution
Проєкт складається з двох частин[cite: 2]:
* `Cli` — консольна точка входу.
* `Core` — бібліотека класів спільної логіки[cite: 2]. 
Згідно з домовленістю на семестр, у `Core` будуть розміщені наступні каталоги: `Core/Dto` (формати даних), `Core/Domain` (сутності з поведінкою) та `Core/Storage` (реалізації сховищ)[cite: 2].

## Команди
* Збірка: `dotnet build`[cite: 1, 3]
* Запуск за замовчуванням: `dotnet run --project src/Cli -f net10.0` (звіт `report-top`; інші підкоманди — розділ «лабораторна 7»)[cite: 1, 3]
* Базова публікація: `dotnet publish src/Cli -c Release -r win-x64`[cite: 1, 3]

## Доменна модель (лабораторна 4)
Каталог `src/Core/Domain` містить сутності з поведінкою; records із `Core/Dto` лишаються форматом даних (DTO).
Домен не залежить від `Console`, `File` та проєкту `Cli`.

* `Product` — товар із залишком: `Create`, `RegisterArrival`, `Issue`, `ChangePrice`, `ChangeStatus`, `ToDto` / `FromDto`.
* `Warehouse` — склад: `Create`, `Relocate`, `ToDto` / `FromDto`.
* `ProductStatus` — `Active`, `Suspended`, `Discontinued` (допустимі переходи перевіряє `switch`-вираз).
* `WarehousePlacementService` — правило на дві сутності (склад + товар); у тижні 5 його викличе `CatalogService`.
* `DomainConverter` (`Core/Import`) — перетворює результат імпорту на сутності й повертає перелік відхилених рядків.

### Інваріанти
Конструктори приватні, публічних `set` немає; створення — лише через фабричні методи, `FromDto` проходить ті самі перевірки.

**Product**
1. Id не порожній (`ArgumentException`, `Create`); зберігається у верхньому регістрі без пробілів по краях.
2. Назва не порожня (`ArgumentException`, `Create`).
3. Ціна не від'ємна (`ArgumentOutOfRangeException`, `Create`, `ChangePrice`).
4. Початковий залишок не від'ємний (`ArgumentOutOfRangeException`, `Create`).
5. Кількість приходу/видачі більша за нуль (`ArgumentOutOfRangeException`, `RegisterArrival`, `Issue`).
6. Не видати більше, ніж є на складі (`InvalidOperationException`, `Issue`).
7. Залишок не переповнює `int` (`InvalidOperationException`, `RegisterArrival`).
8. Операції (прихід, видача, зміна ціни) лише для статусу `Active` (`InvalidOperationException`).
9. Допустимі переходи статусів: `Active ↔ Suspended`, будь-який → `Discontinued`; зі `Discontinued` виходу немає (`InvalidOperationException`, `ChangeStatus`).
10. Статус `FromDto` має бути відомим значенням (`ArgumentException`).
15. Одиниця виміру (`Unit`, лабораторна 7) не порожня (`ArgumentException`, `Create`); за замовчуванням `шт`, для старих файлів без одиниці `FromDto` теж підставляє `шт`.

**Warehouse**
11. Id, назва й розташування не порожні (`ArgumentException`, `Create`, `Relocate`).
12. Не переносити склад на ту саму адресу (`InvalidOperationException`, `Relocate`).

**WarehousePlacementService**
13. На складі не більше N різних товарів (`InvalidOperationException`, `Place`).
14. Один товар зберігається лише на одному складі (`InvalidOperationException`, `Place`).

Чому правила 13–14 винесені в сервіс: жодна сутність не знає про інші екземпляри, а перевірка потребує списку всіх розміщень.
Якби `Warehouse` тримав посилання на всі товари, виник би зв'язок «усі з усіма» й дублювання стану.

Запуск демонстрації: `dotnet run --project src/Cli` (файл за замовчуванням `data/lab04.json`; можна передати шлях до `.csv` або `.json`).

## Сервісний шар (лабораторна 5)
Бізнес-логіка не залежить від способу зберігання: сервіс працює з інтерфейсом, а конкретну реалізацію вибирає точка входу.

* Інтерфейс: **`ICatalogStore`** (`Core/Abstractions`) — `List`, `GetById`, `Add`, `Update`, `Remove`.
* Реалізації (`Core/Storage`): **`InMemoryCatalogStore`** (словник у пам'яті), **`FileCatalogStore`** (JSON-файл зі списком `ProductDto`), **`CachingCatalogStore`** (декоратор над будь-яким іншим сховищем).
* Сервіс: **`CatalogService`** (`Core/Services`) — `Add`, `Receive` (прихід), `Issue` (видача), `ChangePrice`, `Remove`, `All`, `Find`, `Search(Func<Product, bool>)`.
* `Core/SampleData.cs` — демонстраційні товари для сховища в пам'яті (з лабораторної 7 — 20 записів, 4 одиниці виміру).

Схема залежностей: `Cli (Program.cs) → CatalogService → ICatalogStore → InMemoryCatalogStore | FileCatalogStore (| CachingCatalogStore → будь-яке з них)`.
Composition root — `src/Cli/Program.cs`: лише там з'являється `new FileCatalogStore(...)` / `new InMemoryCatalogStore(...)`. Контейнер DI не підключено.

Правила розподілу перевірок: інваріанти товару — у `Product`, у сховищі лише унікальність id (`Add`) та наявність запису (`Update`).

### Запуск
* `dotnet run --project src/Cli` — `InMemoryCatalogStore`, дані зникають після виходу.
* `dotnet run --project src/Cli -- --file` — `FileCatalogStore`, файл `data/catalog.json` створюється поруч зі збіркою (`<каталог збірки>/data/catalog.json`, повний шлях виводиться в консоль); повторні запуски накопичують записи.
* `-- --cache` додає декоратор `CachingCatalogStore` (у кінці друкується статистика кешу); `-- --factory` вибирає сховище через `StoreFactory`; `-- --lab4` запускає демонстрацію лабораторної 4.

### Фабрика чи все в Program.cs
Усе в `Program.cs` — вибір видно в одному місці, нуль додаткових типів; для двох реалізацій цього досить.
`StoreFactory` прибирає логіку вибору з `Program.cs` і дає одне місце для зміни, якщо прапорців побільшає, але конкретні класи тепер розкидані по двох файлах, а для трьох аргументів це зайвий шар.

## Доменні події, делегати Action/Func, лямбди (лабораторна 6)
Сервіс повідомляє зовнішній код про зміни стану, але не знає, хто і як оброблятиме ці сповіщення.
У `src/Core` немає ні `Console`, ні запису журналу у файл: усе це робить `Cli` в обробниках.

Нові/змінені файли:
* `src/Core/Events/CatalogChangeKind.cs` — `enum`: `Added`, `Received`, `Issued`, `Removed`, `Repriced`.
* `src/Core/Events/CatalogChangedEventArgs.cs` — дані події (`: EventArgs`): `Id`, `Kind`, `Delta`, `QuantityAfter`, `PriceAfter`, `At`.
* `src/Core/Services/CatalogService.cs` — `event EventHandler<CatalogChangedEventArgs>? Changed`, приватний `OnChanged`, `FindAll(Func<Product, bool>)`.
* `src/Cli/Program.cs` — підписка (`+=`), другий підписник-лямбда, відписка (`-=`), журнал `logs/app.log`.
* `src/Cli/Notifications/ChangeLine.cs` — спільний формат рядка журналу для консолі й файлу.

### 1. Які зміни стану є подіями і чому
Подією вважається кожна **успішна** операція, що змінює каталог: додавання товару (`Added`), прихід (`Received`),
видача (`Issued`), видалення (`Removed`) і зміна ціни (`Repriced`). Саме ці операції змінюють те, що побачить
користувач у таблиці залишків (тиждень 12) і що має потрапити в журнал (тиждень 10). Читання (`All`, `Find`, `FindAll`)
стану не змінює, тому подій не дає. `Delta` — зміна кількості: `+` для приходу/додавання, `−` для видачі, `0` для
видалення та зміни ціни (для `Repriced` нова ціна передається в `PriceAfter`).

Подія піднімається лише **після** зміни стану й запису у сховище: якщо інваріант `Product` (тиждень 4) кинув виняток,
рядок з `OnChanged` не виконується — повідомлення про зміну, якої не було, не виникає.

### 2. Оголошення події та `OnChanged`
```csharp
public event EventHandler<CatalogChangedEventArgs>? Changed;

private void OnChanged(Product p, CatalogChangeKind kind, int delta)
{
    var args = new CatalogChangedEventArgs(p.Id, kind, delta, p.Quantity, p.Price);
    Changed?.Invoke(this, args);          // ?. — підписників може не бути
    foreach (INotificationSink sink in _sinks)
        sink.Notify(args);                // додаткове завдання 1
}
```
`OnChanged` викликається останнім рядком кожного з методів `Add`, `Receive`, `Issue`, `ChangePrice`, `Remove`.

### 3. Підписники зараз і надалі
* Зараз (`Cli`): локальний метод `OnCatalogChanged` — друкує рядок у консоль і дописує його в `logs/app.log`;
  лямбда-лічильник `(_, _) => changes++` — замикання над локальною змінною `changes`.
* Тиждень 10: логер проєкту `Api` (`ILogger`) — підпишеться на ту саму подію, `Core` не зміниться.
* Тиждень 12: компонент Blazor — у обробнику оновлюватиме таблицю (`StateHasChanged`).
* Тиждень 8: тест підпишеться на `Changed` і перевірить, що операція дала рівно одне сповіщення.

### 4. Сценарій у `Cli`
1. Підписка `OnCatalogChanged` і лямбди-лічильника.
2. Успішні операції (`Add`, `Receive`, `Issue`, `ChangePrice`, `Add` + `Remove`) — по одному рядку події на кожну, потім `Подій отримано: N`.
3. Тимчасовий третій обробник (лямбда, збережена у змінній `orderProbe`) показує порядок: рядок уже надруковано (1-й обробник), лічильник уже збільшено (2-й); потім знімається через `-=`.
4. Відмови в `try/catch` (`ConsoleScenario.TryDo`): від'ємна кількість, неіснуючий id, видача понад залишок тощо — є текст помилки, рядка події немає, `Подій під час відмов: 0`.
5. `service.Changed -= OnCatalogChanged;` → `Receive` виконується (залишок змінився), але рядка в консолі й у `app.log` немає; лічильник +1 (лямбда досі підписана).
6. `service.Changed -= (_, _) => changes++;` нічого не знімає: це новий екземпляр делегата — лічильник і далі зростає.
7. `service.FindAll(p => p.Quantity < 10)` — потрібне значення у відповідь, тому `Func`, а не подія.

Формат рядка: `$"{e.At:HH:mm:ss} {e.Kind,-8} {e.Id} {e.Delta,4:+0;-0} → залишок {e.QuantityAfter}"`. Очікуваний вигляд:
```
14:20:31 Added    9F2C1AB4  +50 → залишок 50
14:20:31 Received 9F2C1AB4  +25 → залишок 75
14:20:31 Issued   9F2C1AB4  -10 → залишок 65
14:20:31 Repriced 9F2C1AB4   +0 → залишок 65, нова ціна 31.00
...
Подій отримано: 6
```
Журнал: `<каталог збірки>/logs/app.log` (шлях через `Path.Combine(AppContext.BaseDirectory, "logs", "app.log")`,
каталог створює `Directory.CreateDirectory`; повний шлях друкується на старті). Кожен запуск дописує рядок-заголовок
`# запуск ...`, далі — лише рядки подій до відписки. Файли `*.log` ігноруються git.

### 5. Подія чи прямий виклик логера
Якби `CatalogService` сам викликав `Console.WriteLine` чи `File.AppendAllText`, (1) `Core` отримав би залежність від
конкретного способу виведення: для логера `Api` (тиждень 10) і для Blazor (тиждень 12) довелося б змінювати й перекомпільовувати
доменну бібліотеку, а консольні рядки з'явилися б і у веб-застосунку; (2) тест не зміг би перевірити факт зміни інакше, ніж
читаючи консоль або файл, і кожен запуск тесту засмічував би журнал. З подією `Core` лише каже «що сталося», а кількість і поведінка
одержувачів визначаються ззовні: їх можна додавати (`+=`) і прибирати (`-=`) під час роботи без змін у сервісі.

Обмеження події: обробники викликаються синхронно і по черзі; виняток у першому обробнику не дасть виконатися наступним і
повернеться у викликача бізнес-операції (хоча стан уже змінено). Тому ризикований запис у файл обгорнуто `try/catch` усередині обробника.
Підписник, якого забули відписати, тримається сервісом у пам'яті (витік) — у консольній програмі, що завершується разом із сервісом, це некритично.

### Додаткове 1: `INotificationSink` (ручний DI)
* Контракт `Core/Abstractions/INotificationSink.cs` (`void Notify(CatalogChangedEventArgs change)`); реалізації — у `Cli/Notifications`:
  `ConsoleSink` (екран, префікс `[ConsoleSink]`) і `FileSink` (файл `logs/sink.log`, помилку запису перехоплює сам).
* `CatalogService(ICatalogStore store, params INotificationSink[] sinks)` — одержувачі передаються в конструктор у `Program.cs`:
  `new CatalogService(store, new ConsoleSink(), new FileSink(sinkLogPath))`.

| | Подія `Changed` | `INotificationSink` у конструкторі |
|---|---|---|
| Хто визначає одержувачів | підписники самі, під час роботи (`+=` / `-=`) | composition root, один раз при створенні сервісу |
| Залежність видно з сигнатури | ні — сервіс можна створити без підписників | так — одержувачі є параметрами конструктора |
| Відписка | `-=` тим самим делегатом | неможлива без створення нового сервісу |
| Зручність для DI-контейнера (тиждень 10) | обробник треба підписати вручну | реєструються як звичайні сервіси |
| Підходить для | UI, тести, тимчасові спостерігачі | постійна інфраструктура (журнал, аудит) |

### Додаткове 2: `Action<string>` у методі імпорту
`ProductCsvImporter.Load(path, Action<string>? onSkipped = null)`, `ProductJsonImporter.Load(path, onSkipped)` і
`DomainConverter.ToDomain(source, Action<string>? onRejected = null)` повідомляють про кожен пропущений рядок через делегат;
список `Errors` у результаті лишається, як і раніше. У `Cli` передається лямбда, яка друкує причину й рахує пропуски (замикання над `skipped`).
Файл для імпорту — перший аргумент `*.csv`/`*.json`, інакше `data/sample.csv` (шукається в поточному каталозі та вище від каталогу збірки).

### Запуск
З лабораторної 7 сценарій подій винесено з `Program.cs` у `src/Cli/EventsDemo.cs` і запускається підкомандою `lab6`
(за замовчуванням `Program.cs` тепер виконує звіт `report-top`):
* `dotnet run --project src/Cli -f net10.0 -- lab6` — сценарій лабораторної 6 на `InMemoryCatalogStore`.
* `dotnet run --project src/Cli -f net10.0 -- lab6 --file` / `--cache` / `--factory` — те саме з іншим сховищем (як у лабораторній 5).
* `dotnet run --project src/Cli -f net10.0 -- lab6 data/sample.json` — імпорт іншого файлу в додатковому завданні 2.

Самоперевірка: пошук у `src/Core` за `Console` і за `logs` нічого не знаходить.

## Звіти через LINQ: фільтри, проєкції, групування, топ-N, агрегати (лабораторна 7)
Аналітика над колекцією товарів — лише LINQ to Objects (без PLINQ і без LINQ to SQL). Усі запити — у `Core/Reports/ReportService`,
результати — record-DTO, а не `Product`. У `Program.cs` немає жодного `Where`/`GroupBy`: лише вибір підкоманди та вивід (`ReportPrinter`).

Нові/змінені файли:
* `src/Core/Reports/StockLine.cs`, `GroupSummary.cs`, `StockValueLine.cs`, `StockMovement.cs` — DTO звітів (record).
* `src/Core/Reports/ReportService.cs` — усі запити; `EnumerableExtensions.cs` — розширення `TopBy`; `ReportVariants.cs` — варіанти для вимірювань.
* `src/Core/Domain/Product.cs`, `Dto/ProductDto.cs` — нова властивість `Unit` (одиниця виміру, інваріант 15); CSV-імпорт приймає шосту колонку `unit`.
* `src/Core/SampleData.cs` — 20 записів, 4 одиниці (`шт` ×9, `м` ×4, `уп` ×4, `компл` ×3), 6 позицій із залишком < 10; `Generate(n)` — великий набір.
* `src/Cli/Program.cs` — підкоманди `report-*`; `ReportPrinter.cs` — вивід; `DeferredExecutionDemo.cs`, `MovementsDemo.cs`, `BenchmarkDemo.cs`;
  сценарій лабораторної 6 перенесено в `EventsDemo.cs` (підкоманда `lab6`).

### 1. Перелік запитів
| Метод | Питання | Оператори |
|---|---|---|
| `LowStock(source, threshold)` | Яких товарів менше за поріг (дефіцит)? | `Where` → `OrderBy` + `ThenBy` → `Select` у `StockLine` → `ToList` |
| `TopByQuantity(source, n)` | Яких товарів на складі найбільше? | `TopBy` (`OrderByDescending` + `ThenBy` за `Id` + `Take`) → `Select` → `ToList` |
| `ByUnit(source)` | Як залишки розподіляються за одиницями виміру? | `GroupBy` (з `OrdinalIgnoreCase`) → `Select` (`Key`, `Count`, `Sum`) → `OrderByDescending` + `ThenBy` |
| `Totals(source)` | Скільки позицій, сумарний, середній і максимальний залишок? | `ToList`, `Count`, `Sum`, `Average`, `Max`; іменований кортеж |
| `TopByValue(source, n)` | Де «заморожено» найбільше грошей (ціна × кількість)? | `TopBy` → `Select` у `StockValueLine` |
| `ByUnitQuerySyntax(source)` | Те саме, що `ByUnit`, у синтаксисі запитів (дод. 3) | `from … group … by … into g`, `let`, `orderby … descending`, `select` |
| `MovementJournal(products, movements)` | Які були рухи і по яких товарах (дод. 1)? | `Join` → `MovementLine` |
| `MovementsByProduct(products, movements)` | Скільки рухів, приходу й видачі по кожному товару (дод. 1)? | `GroupJoin`, `Count`, `Where` + `Sum` |

Порожнє джерело: `LowStock`/`TopBy…`/`ByUnit` повертають порожній список (на порожній послідовності `Where`, `OrderBy`, `GroupBy` винятків не дають),
`Totals` перевіряє `list.Count == 0` і повертає `(0, 0, 0d, 0)` — без виклику `Average`/`Max`, які кинули б `InvalidOperationException`.

### 2. GroupBy з проєкцією
```csharp
public static IReadOnlyList<GroupSummary> ByUnit(IEnumerable<Product> source) =>
    source.GroupBy(p => p.Unit, StringComparer.OrdinalIgnoreCase)              // ключ — одиниця; "уп" і "УП" — одна група
        .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity))) // g.Key — ключ, g.Count() — позицій у групі
        .OrderByDescending(s => s.TotalQuantity).ThenBy(s => s.Key, StringComparer.Ordinal)
        .ToList();
```
У `SampleData` товар `P-016` навмисно має одиницю `УП` (великими): завдяки `OrdinalIgnoreCase` він потрапляє в групу `уп`, а не в окрему.

### 3. Вивід підкоманд
Запуск: `dotnet run --project src/Cli -f net10.0 -- <підкоманда>` (без підкоманди — `report-top`). Очікуваний вивід на `SampleData`
(розраховано за даними; десятковий роздільник залежить від регіональних налаштувань Windows):
```
> dotnet run --project src/Cli -f net10.0 -- report-top
=== Топ-5 за залишком ===
  Id        Назва                                 Залишок  Од.
  P-010     Кабель UTP cat6                          1000  м
  P-005     Кабель HDMI 2 м                           300  шт
  P-011     Кабель живлення ПВС 3×1.5                 250  м
  P-012     Гофротруба 20 мм                          150  м
  P-002     Мишка бездротова                          120  шт

> dotnet run --project src/Cli -f net10.0 -- report-low
=== Дефіцит: залишок < 10 ===
  Id        Назва                                 Залишок  Од.
  P-015     Картридж чорний (уп. 2 шт)                  0  уп
  P-007     Планшет попередньої моделі                  3  шт
  P-009     Принтер лазерний                            5  шт
  P-019     Набір інструментів монтажника               7  компл
  P-001     Ноутбук Dell Latitude                       8  шт
  P-013     Кабель-канал 25×16                          9  м

> dotnet run --project src/Cli -f net10.0 -- report-groups
=== Групування за одиницею виміру ===
  м      позицій:   4   сумарно:   1409
  шт     позицій:   9   сумарно:    586
  уп     позицій:   4   сумарно:    134
  компл  позицій:   3   сумарно:     44

> dotnet run --project src/Cli -f net10.0 -- report-sum
=== Підсумки ===
  позицій 20, всього 2173, середнє 108,65, макс 1000

> dotnet run --project src/Cli -f net10.0 -- report-value
=== Топ-5 за вартістю залишку (ціна × кількість) ===
  Id        Назва                                 Залишок Од.         Ціна     Вартість
  P-001     Ноутбук Dell Latitude                       8 шт       1250,50     10004,00
  P-020     Кріплення для монітора VESA                25 компл     380,00      9500,00
  P-003     Клавіатура механічна                       45 шт        140,00      6300,00
  P-006     Навушники                                  60 шт         85,99      5159,40
  P-019     Набір інструментів монтажника               7 компл     650,00      4550,00
```
Інші підкоманди: `report-all` (п'ять звітів поспіль), `report-groups-query`, `report-deferred`, `report-moves`, `report-bench`, `lab6`.
Прапорці: `--file`, `--cache`, `--factory`, `--empty` (порожнє джерело: `report-low` друкує «(немає записів)», `report-sum` — нулі).
Невідома підкоманда — довідка і код повернення 1.

### 4. Відкладене виконання (`report-deferred`)
```
Запит побудовано; умову перевірено разів: 0 (запит ще не виконувався)
Дефіцитних до змін: 6   (перевірок: 20)
Дефіцитних після змін: 7   (перевірок: 41) — запит виконано ще раз, новий елемент потрапив у результат
Запит після source.Clear(): 0
У знімку лишилось: 7   — список після ToList від джерела не залежить
```
Рядок `source.Where(...)` лише будує опис запиту: лямбда-умова не викликалась жодного разу (лічильник перевірок 0).
Запит виконується щоразу, коли його перебирають, — тут кожним викликом `Count()`. Перший `Count()` пройшов 20 товарів і знайшов 6 дефіцитних;
після `source.Add(...)` другий `Count()` виконав той самий запит заново вже над 21 елементом (ще 21 перевірка, разом 41) і знайшов 7.
`ToList()` матеріалізує результат: знімок (`snapshot.Count` — властивість, а не повторний запит) лишається 7 і після `source.Clear()`.
Звіти в `ReportService` завершуються `ToList()`, тому кожен звіт читає джерело рівно один раз — у момент виклику методу, а не під час виводу в `Cli`.

### Додаткове 1: Join і GroupJoin (`report-moves`)
Друга колекція — `StockMovement` (рух товару): `MovementsDemo` підписується на `CatalogService.Changed` (лабораторна 6) і з кожної події робить запис руху.
Виконується кілька операцій (одна з них — прихід на призупинений принтер — відхиляється інваріантом і руху не дає), а також додається й видаляється тимчасовий товар.
* `Join` (внутрішнє з'єднання) — журнал руху з назвами: 2 записи руху видаленого товару не мають пари в каталозі й у звіт не потрапляють.
* `GroupJoin` — рядок для **кожного** товару, навіть без руху (`Рухів = 0`), з сумами приходу й видачі.

### Додаткове 2: метод-розширення `TopBy`
`TopBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> key, int n, Func<T, string> tieBreaker)` — `OrderByDescending(key).ThenBy(tieBreaker, Ordinal).Take(n)`.
Використовується у двох звітах: `TopByQuantity` (ключ `int`) і `TopByValue` (ключ `decimal`). Другий ключ сортування обов'язковий параметр,
тож «забути ThenBy» неможливо; аргументи перевіряються одразу (метод не ітератор), а сам запит лишається відкладеним до `ToList` у звіті.

### Додаткове 3: синтаксис методів чи синтаксис запитів (`report-groups-query`)
```csharp
from product in source
group product by product.Unit.ToLowerInvariant() into g
let total = g.Sum(p => p.Quantity)
orderby total descending, g.Key
select new GroupSummary(g.Key, g.Count(), total)
```
Обидва варіанти дають однаковий результат (Cli друкує `Результати однакові: True` — record порівнюються за значенням).
Синтаксис запитів для групування читається як речення і дає `let` — ім'я проміжному значенню, тому сума рахується один раз.
Мінуси: у `group … by` не можна передати `StringComparer`, тож ключ доводиться нормалізувати вручну (`ToLowerInvariant`), а в `orderby` —
неможливо задати `Ordinal`; `Take`, `ToList` і власний `TopBy` у синтаксисі запитів немає — все одно потрібні дужки й виклик методу.
Для ланцюжків «фільтр → сортування → топ-N» синтаксис методів коротший і читається зліва направо; для групування з проміжними значеннями — синтаксис запитів наочніший.

### Додаткове 4: Stopwatch на 100 000 записів (`report-bench`)
`SampleData.Generate(100_000)` (фіксований seed → однакові дані). Для кожного варіанта — прогрівальний виклик (JIT) і 10 вимірів, друкуються середнє і мінімум.
Порівнюються три варіанти одного звіту `LowStock` (`ReportVariants`):
* А — один ланцюжок, `ToList` лише в кінці;
* Б — `ToList` після кожного кроку (ті самі операції + проміжні списки);
* В — `OrderBy` → `ToList` → `Where`: сортуються всі 100 000 записів, а потім ~99 % відкидається.

Очікування: А і Б близькі (фільтр відкидає ~99 %, тому проміжні списки маленькі; Б трохи повільніший через зайві копіювання),
В — у рази повільніший, бо `OrderBy` — буферизувальний оператор і сортує всю послідовність (O(n log n) над 100 000 замість ~1 000).
Отже, у ланцюжку важливіше **порядок** операторів (фільтр — перед сортуванням), ніж наявність проміжного `ToList`.
Результати вимірювання (заповнити після запуску `dotnet run --project src/Cli -c Release -f net10.0 -- report-bench`):

| Варіант | Рядків | Середнє, мс | Мін, мс |
|---|---|---|---|
| А. один ланцюжок | | | |
| Б. ToList після кожного кроку | | | |
| В. OrderBy → ToList → Where | | | |

### Самоперевірка
1. У `Program.cs` немає `Where`/`GroupBy`/`OrderBy` — лише виклики `ReportService` і `ReportPrinter`.
2. Два запуски `report-top` дають однаковий порядок (другий ключ — унікальний `Id`).
3. `-- report-low --empty` і `-- report-sum --empty` не падають.
4. Кожен звіт повертає DTO (`StockLine`, `GroupSummary`, `StockValueLine`, `MovementLine`, `MovementSummary`) або кортеж, а не `Product`.
5. `AsParallel` у проєкті немає.

## Мультитаргетинг (Multi-targeting)
Проєкт налаштовано для збірки під кілька цільових фреймворків (TFM)[cite: 2]. Завдяки умовній компіляції (`#if NET10_0_OR_GREATER`), програма виводить різні дані залежно від версії[cite: 2]. 
Для запуску конкретної версії використовуйте прапорець `-f`:
* `dotnet run --project src/Cli -f net10.0`
* `dotnet run --project src/Cli -f net8.0`
Мультитаргетинг не вдався (залишено лише net10.0), оскільки в системі встановлено лише .NET SDK 10, а targeting pack для net8.0 недоступний.

## Аналіз режимів публікації
Різниця між базовими режимами[cite: 2]:
* **Framework-dependent**: містить лише код застосунку, займає мінімум місця, але вимагає встановленого .NET Runtime[cite: 2].
* **Self-contained**: включає середовище виконання .NET, працює автономно на будь-якій машині з відповідною ОС, але має великий розмір[cite: 2].

| RID | Режим | Розмір | Примітки |
|---|---|---|---|
| win-x64 | framework-dependent | ~0.2 МБ | Потрібен runtime (.NET 10)[cite: 2] |
| win-x64 | self-contained | 76.7 МБ | Автономний, runtime не потрібен[cite: 2] |
| win-x64 | single-file | 70.1 МБ | Усі залежності об'єднані в один файл[cite: 2] |
| win-x64 | trimmed | 19.1 МБ | Видалено весь невикористаний системний код[cite: 2] |

**Небезпека Trimming:** Параметр `PublishTrimmed=true` суттєво зменшує розмір публікації, але він небезпечний для коду, який використовує рефлексію[cite: 2]. Компілятор не здатний передбачити, які саме типи чи методи викликатимуться динамічно, тому може видалити критично важливі компоненти[cite: 2].

## Середовище
.NET SDK 10.0, Windows 10 x64[cite: 1, 3]

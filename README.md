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
* Запуск за замовчуванням: `dotnet run --project src/Cli`[cite: 1, 3]
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
* `Core/SampleData.cs` — 18 демонстраційних товарів для сховища в пам'яті.

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
* `dotnet run --project src/Cli` — сценарій лабораторної 6 на `InMemoryCatalogStore`.
* `dotnet run --project src/Cli -- --file` / `-- --cache` / `-- --factory` — те саме з іншим сховищем (як у лабораторній 5).
* `dotnet run --project src/Cli -- data/sample.json` — імпорт іншого файлу в додатковому завданні 2.

Самоперевірка: пошук у `src/Core` за `Console` і за `logs` нічого не знаходить.

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

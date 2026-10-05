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

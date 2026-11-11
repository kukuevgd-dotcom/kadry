# Архитектура

Приложение строится по паттерну MVVM. Решение — два проекта:

- `Kadry.Core` (net10.0) — предметная модель, доступ к данным, бизнес-правила. Не зависит от WPF.
- `Kadry.App` (net10.0-windows) — интерфейс WPF: окна, страницы, модели представления.
- `Kadry.Core.Tests` — тесты Core на xUnit.

Зависимости: App → Core, Tests → Core.

## Слои

**View** (`Kadry.App/Views`) — XAML-разметка. Логики нет: привязки к свойствам ViewModel и команды. Стили и шаблоны в `App.xaml`.

**ViewModel** (`Kadry.App/ViewModels`) — состояние экрана и команды:
- `MainViewModel` — навигация: текущий раздел, меню по роли;
- `EmployeesViewModel`, `RequestsViewModel`, `ScheduleViewModel`, `DirectoriesViewModel`, `ReportsViewModel` — по экрану;
- `ViewModelBase` — `INotifyPropertyChanged` + `SetProperty`;
- `RelayCommand` — `ICommand` с делегатами.

**Model** (`Kadry.Core`) — всё, что не про интерфейс:
- `Domain/Entities` — сущности EF Core: `Employee`, `Department`, `Position`, `VacationType`, `VacationRequest`, `VacationRecord`, `User`;
- `Domain/Enums` — `RequestStatus`, `UserRole`;
- `Data/KadryContext` — контекст EF Core (SQLite);
- `Data/DbInitializer` — начальное заполнение справочников;
- `Services/RequestRules` — матрица переходов статусов, расчёт календарных дней, расчёт остатков, проверка пересечений;
- `Services/PasswordHasher` — PBKDF2;
- `Services/*Service` — операции сценариев (заявления, сотрудники, отчёты).

## Поток выполнения (пример: утверждение заявления)

1. Кнопка «Утвердить» в `RequestDialog` → `ApproveCommand` у `RequestsViewModel`.
2. `RequestsViewModel` → `VacationRequestService.Approve(requestId)`.
3. Сервис: проверяет статус (только из «Согласовано»), считает дни периода, создаёт `VacationRecord`, ставит статус «Утверждено», сохраняет всё одной транзакцией.
4. ViewModel перечитывает состояние журнала → уведомления `INotifyPropertyChanged` обновляют таблицу.

## Ключевые решения

- MVVM реализуется вручную (без сторонних библиотек), чтобы паттерн был виден в коде.
- Доступ к данным — EF Core Code First + SQLite (WAL); миграции в репозитории.
- Остатки дней вычисляются из записей, кэша нет.
- Ошибки бизнес-правил — исключения с текстом для пользователя, перехватываются в ViewModel и показываются диалогом.
- Права проверяются в сервисах (не только в интерфейсе): роль передаётся в вызов.

## Структура каталогов

```
docs/            — документация (схема БД, архитектура, отчёт)
src/
  Kadry.Core/    — модели, контекст, сервисы
  Kadry.App/     — WPF-интерфейс
tests/
  Kadry.Core.Tests/
```
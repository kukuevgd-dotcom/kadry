using System.Collections.ObjectModel;
using Kadry.App.Infrastructure;
using Kadry.App.Services;
using Kadry.Core.Services;

namespace Kadry.App.ViewModels;

/// <summary>Строка отчёта «Движение кадров» для таблицы.</summary>
public class MovementRowView
{
    public string Employee { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string Event { get; init; } = string.Empty;
    public string EventDate { get; init; } = string.Empty;
}

/// <summary>Экран «Отчёты»: остатки дней и движение кадров, экспорт CSV.</summary>
public class ReportsViewModel : ViewModelBase
{
    private readonly ReportService _reportService;
    private readonly DirectoryService _directoryService;
    private readonly IUiDialogs _dialogs;

    private int _year = DateTime.Today.Year;
    private DepartmentFilterItem? _departmentFilter;
    private DateTime _movementFrom = new(DateTime.Today.Year, 1, 1);
    private DateTime _movementTo = new(DateTime.Today.Year, 12, 31);

    public ReportsViewModel(ReportService reportService,
                            DirectoryService directoryService,
                            IUiDialogs dialogs)
    {
        _reportService = reportService;
        _directoryService = directoryService;
        _dialogs = dialogs;

        Years = Enumerable.Range(DateTime.Today.Year - 2, 5).ToList();

        ExportRemainingCommand = new RelayCommand(_ => ExportRemaining());
        ExportMovementCommand = new RelayCommand(_ => ExportMovement());

        ReloadFilters();
        Reload();
    }

    public ObservableCollection<RemainingRow> RemainingRows { get; } = new();

    public ObservableCollection<MovementRowView> MovementRows { get; } = new();

    public ObservableCollection<DepartmentFilterItem> DepartmentFilters { get; } = new();

    public IReadOnlyList<int> Years { get; }

    public RelayCommand ExportRemainingCommand { get; }

    public RelayCommand ExportMovementCommand { get; }

    public int Year
    {
        get => _year;
        set
        {
            if (SetProperty(ref _year, value))
                Reload();
        }
    }

    public DepartmentFilterItem? DepartmentFilter
    {
        get => _departmentFilter;
        set
        {
            if (SetProperty(ref _departmentFilter, value))
                Reload();
        }
    }

    public DateTime MovementFrom
    {
        get => _movementFrom;
        set
        {
            if (SetProperty(ref _movementFrom, value))
                ReloadMovements();
        }
    }

    public DateTime MovementTo
    {
        get => _movementTo;
        set
        {
            if (SetProperty(ref _movementTo, value))
                ReloadMovements();
        }
    }

    private void ReloadFilters()
    {
        int selectedId = _departmentFilter?.Id ?? 0;

        DepartmentFilters.Clear();
        DepartmentFilters.Add(new DepartmentFilterItem(0, "Все отделы"));
        foreach (var department in _directoryService.Departments())
            DepartmentFilters.Add(new DepartmentFilterItem(department.Id, department.Name));

        _departmentFilter = DepartmentFilters.FirstOrDefault(d => d.Id == selectedId);
        OnPropertyChanged(nameof(DepartmentFilter));
    }

    private int? DepartmentId => _departmentFilter is { Id: > 0 } filter ? filter.Id : null;

    private void Reload()
    {
        ReloadRemaining();
        ReloadMovements();
    }

    private void ReloadRemaining()
    {
        RemainingRows.Clear();
        foreach (var row in _reportService.RemainingDays(_year, DepartmentId))
            RemainingRows.Add(row);
    }

    private void ReloadMovements()
    {
        MovementRows.Clear();
        foreach (var row in _reportService.Movements(_movementFrom, _movementTo, DepartmentId))
        {
            MovementRows.Add(new MovementRowView
            {
                Employee = row.Employee,
                Department = row.Department,
                Event = row.Event,
                EventDate = row.EventDate.ToString("dd.MM.yyyy")
            });
        }
    }

    private void ExportRemaining()
    {
        if (RemainingRows.Count == 0)
        {
            _dialogs.Error("Отчёт пуст — экспортировать нечего.");
            return;
        }

        string? path = _dialogs.SaveFile($"Остатки дней {_year}.csv");
        if (path is null)
            return;

        try
        {
            ReportService.WriteRemainingCsv(path, RemainingRows.ToList());
        }
        catch (Exception ex)
        {
            _dialogs.Error($"Не удалось сохранить файл: {ex.Message}");
        }
    }

    private void ExportMovement()
    {
        if (MovementRows.Count == 0)
        {
            _dialogs.Error("Отчёт пуст — экспортировать нечего.");
            return;
        }

        string? path = _dialogs.SaveFile(
            $"Движение кадров {_movementFrom:dd.MM.yyyy}-{_movementTo:dd.MM.yyyy}.csv");
        if (path is null)
            return;

        try
        {
            ReportService.WriteMovementCsv(path,
                _reportService.Movements(_movementFrom, _movementTo, DepartmentId));
        }
        catch (Exception ex)
        {
            _dialogs.Error($"Не удалось сохранить файл: {ex.Message}");
        }
    }
}

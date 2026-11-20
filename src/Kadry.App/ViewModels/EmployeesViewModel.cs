using System.Collections.ObjectModel;
using Kadry.App.Infrastructure;
using Kadry.App.Services;
using Kadry.Core.Domain;
using Kadry.Core.Services;

namespace Kadry.App.ViewModels;

/// <summary>Строка списка сотрудников.</summary>
public class EmployeeRow
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string PersonnelNumber { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string Position { get; init; } = string.Empty;
    public string HiredAt { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

/// <summary>Пункт фильтра по отделу: 0 — все отделы.</summary>
public record DepartmentFilterItem(int Id, string Name);

/// <summary>Экран «Сотрудники»: список, поиск, фильтры, приём/увольнение.</summary>
public class EmployeesViewModel : ViewModelBase
{
    private readonly EmployeeService _service;
    private readonly DirectoryService _directoryService;
    private readonly IUiDialogs _dialogs;

    private string _searchText = string.Empty;
    private DepartmentFilterItem? _departmentFilter;
    private string _statusFilter = "все";
    private EmployeeRow? _selectedEmployee;

    public EmployeesViewModel(EmployeeService service, DirectoryService directoryService, IUiDialogs dialogs)
    {
        _service = service;
        _directoryService = directoryService;
        _dialogs = dialogs;

        HireCommand = new RelayCommand(_ => Hire());
        EditCommand = new RelayCommand(_ => Edit(), _ => SelectedEmployee is not null);
        FireCommand = new RelayCommand(_ => Fire(), _ => SelectedEmployee is not null
            && SelectedEmployee.Status.StartsWith("работает"));
        DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedEmployee is not null);

        ReloadFilters();
        Reload();
    }

    public ObservableCollection<EmployeeRow> Employees { get; } = new();

    public ObservableCollection<DepartmentFilterItem> DepartmentFilters { get; } = new();

    public IReadOnlyList<string> StatusFilters { get; } = new[] { "все", "работает", "уволен" };

    public RelayCommand HireCommand { get; }

    public RelayCommand EditCommand { get; }

    public RelayCommand FireCommand { get; }

    public RelayCommand DeleteCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
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

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
                Reload();
        }
    }

    public EmployeeRow? SelectedEmployee
    {
        get => _selectedEmployee;
        set => SetProperty(ref _selectedEmployee, value);
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

    private void Reload()
    {
        int? departmentId = _departmentFilter is { Id: > 0 } filter ? filter.Id : null;
        bool? onlyActive = _statusFilter switch
        {
            "работает" => true,
            "уволен" => false,
            _ => null
        };

        Employees.Clear();
        foreach (var e in _service.List(_searchText, departmentId, onlyActive))
        {
            Employees.Add(new EmployeeRow
            {
                Id = e.Id,
                FullName = e.FullName,
                PersonnelNumber = e.PersonnelNumber,
                Department = e.Department?.Name ?? string.Empty,
                Position = e.Position?.Name ?? string.Empty,
                HiredAt = e.HiredAt.ToString("dd.MM.yyyy"),
                Status = e.FiredAt is null
                    ? "работает"
                    : $"уволен {e.FiredAt:dd.MM.yyyy}"
            });
        }
    }

    private void Hire()
    {
        var input = _dialogs.EditEmployee(_directoryService.Departments(), _directoryService.Positions());
        if (input is null)
            return;

        try
        {
            _service.Hire(new Employee
            {
                FullName = input.FullName,
                PersonnelNumber = input.PersonnelNumber,
                DepartmentId = input.DepartmentId,
                PositionId = input.PositionId,
                HiredAt = input.HiredAt
            });
            Reload();
        }
        catch (Exception ex)
        {
            _dialogs.Error(ex.Message);
        }
    }

    private void Edit()
    {
        if (SelectedEmployee is null)
            return;

        var employee = _service.Find(SelectedEmployee.Id);
        if (employee is null)
            return;

        var input = _dialogs.EditEmployee(
            _directoryService.Departments(),
            _directoryService.Positions(),
            new EmployeeInput(
                employee.FullName,
                employee.PersonnelNumber,
                employee.DepartmentId,
                employee.PositionId,
                employee.HiredAt));
        if (input is null)
            return;

        try
        {
            _service.Update(new Employee
            {
                Id = employee.Id,
                FullName = input.FullName,
                PersonnelNumber = input.PersonnelNumber,
                DepartmentId = input.DepartmentId,
                PositionId = input.PositionId,
                HiredAt = input.HiredAt
            });
            Reload();
        }
        catch (Exception ex)
        {
            _dialogs.Error(ex.Message);
        }
    }

    private void Fire()
    {
        if (SelectedEmployee is null)
            return;

        var date = _dialogs.PickDate("Увольнение", DateTime.Today);
        if (date is null)
            return;

        if (!_dialogs.Confirm("Увольнение",
                $"Уволить {SelectedEmployee.FullName} с {date:dd.MM.yyyy}? История сохранится."))
            return;

        try
        {
            _service.Fire(SelectedEmployee.Id, date.Value);
            Reload();
        }
        catch (Exception ex)
        {
            _dialogs.Error(ex.Message);
        }
    }

    private void Delete()
    {
        if (SelectedEmployee is null)
            return;

        if (!_dialogs.Confirm("Удаление",
                $"Удалить сотрудника {SelectedEmployee.FullName}? Действие необратимо."))
            return;

        try
        {
            _service.Delete(SelectedEmployee.Id);
            Reload();
        }
        catch (Exception ex)
        {
            _dialogs.Error(ex.Message);
        }
    }
}

using System.Collections.ObjectModel;
using Kadry.App.Infrastructure;
using Kadry.Core.Services;

namespace Kadry.App.ViewModels;

/// <summary>Ячейка месяца в графике отпусков.</summary>
public class MonthCell
{
    public string Text { get; init; } = string.Empty;
    public bool HasConflict { get; init; }
}

/// <summary>Строка графика: сотрудник и 12 ячеек месяцев.</summary>
public class ScheduleRow
{
    public string EmployeeName { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public List<MonthCell> Cells { get; init; } = Enumerable
        .Range(0, 12)
        .Select(_ => new MonthCell())
        .ToList();
}

/// <summary>Экран «График отпусков»: год, помесячная сетка, подсветка пересечений.</summary>
public class ScheduleViewModel : ViewModelBase
{
    private readonly VacationRequestService _requestService;
    private readonly DirectoryService _directoryService;

    private int _year = DateTime.Today.Year;
    private DepartmentFilterItem? _departmentFilter;

    public ScheduleViewModel(VacationRequestService requestService,
                             DirectoryService directoryService)
    {
        _requestService = requestService;
        _directoryService = directoryService;

        Years = Enumerable.Range(DateTime.Today.Year - 2, 5).ToList();

        ReloadFilters();
        Reload();
    }

    public ObservableCollection<ScheduleRow> Rows { get; } = new();

    public ObservableCollection<DepartmentFilterItem> DepartmentFilters { get; } = new();

    public IReadOnlyList<int> Years { get; }

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
        var records = _requestService.RecordsForYear(_year, departmentId);

        // пересечения внутри подразделения: месяцы, где отпуска коллег накладываются
        var conflictMonths = new HashSet<(int EmployeeId, int Month)>();
        for (int i = 0; i < records.Count; i++)
        {
            for (int j = i + 1; j < records.Count; j++)
            {
                var a = records[i];
                var b = records[j];
                if (a.EmployeeId == b.EmployeeId)
                    continue;

                // оба сотрудника одного отдела?
                if (a.Employee?.DepartmentId != b.Employee?.DepartmentId)
                    continue;

                if (a.StartDate <= b.EndDate && b.StartDate <= a.EndDate)
                {
                    var overlapStart = a.StartDate > b.StartDate ? a.StartDate : b.StartDate;
                    var overlapEnd = a.EndDate < b.EndDate ? a.EndDate : b.EndDate;

                    for (int month = overlapStart.Month; month <= overlapEnd.Month; month++)
                    {
                        conflictMonths.Add((a.EmployeeId, month));
                        conflictMonths.Add((b.EmployeeId, month));
                    }
                }
            }
        }

        Rows.Clear();
        foreach (var group in records.GroupBy(r => r.EmployeeId))
        {
            var row = new ScheduleRow
            {
                EmployeeName = group.First().Employee?.FullName ?? string.Empty,
                Department = group.First().Employee?.Department?.Name ?? string.Empty
            };

            foreach (var record in group)
            {
                // разложить период по месяцам
                var month = record.StartDate.Month;
                var end = record.EndDate;
                var start = record.StartDate;
                while (start <= end)
                {
                    month = start.Month;
                    var monthEnd = new DateTime(start.Year, month, DateTime.DaysInMonth(start.Year, month));
                    var segEnd = end < monthEnd ? end : monthEnd;

                    var cell = row.Cells[month - 1];
                    var segment = $"{start.Day}–{segEnd.Day}";
                    var existing = row.Cells[month - 1].Text;
                    row.Cells[month - 1] = new MonthCell
                    {
                        Text = existing.Length == 0 ? segment : $"{existing}, {segment}",
                        HasConflict = cell.HasConflict
                    };

                    start = segEnd.AddDays(1);
                }
            }

            for (int m = 0; m < 12; m++)
            {
                if (conflictMonths.Contains((group.Key, m + 1)))
                {
                    var cell = row.Cells[m];
                    row.Cells[m] = new MonthCell { Text = cell.Text, HasConflict = true };
                }
            }

            Rows.Add(row);
        }
    }
}

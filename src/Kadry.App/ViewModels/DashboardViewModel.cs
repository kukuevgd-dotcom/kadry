using System.Collections.ObjectModel;
using Kadry.App.Infrastructure;
using Kadry.Core.Services;

namespace Kadry.App.ViewModels;

/// <summary>Строка «отпуска текущего месяца» на панели.</summary>
public class MonthVacationRow
{
    public string Employee { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
    public string Period { get; init; } = string.Empty;
}

/// <summary>Экран «Панель мониторинга»: сводные счётчики и ближайшие события.</summary>
public class DashboardViewModel : ViewModelBase
{
    private readonly ReportService _reportService;

    private int _pendingCount;
    private int _monthVacationCount;
    private int _activeEmployeeCount;

    public DashboardViewModel(ReportService reportService)
    {
        _reportService = reportService;
        Reload();
    }

    public ObservableCollection<RequestRow> PendingRequests { get; } = new();

    public ObservableCollection<MonthVacationRow> MonthVacations { get; } = new();

    public ObservableCollection<DepartmentCount> DepartmentCounts { get; } = new();

    public int PendingCount
    {
        get => _pendingCount;
        private set => SetProperty(ref _pendingCount, value);
    }

    public int MonthVacationCount
    {
        get => _monthVacationCount;
        private set => SetProperty(ref _monthVacationCount, value);
    }

    public int ActiveEmployeeCount
    {
        get => _activeEmployeeCount;
        private set => SetProperty(ref _activeEmployeeCount, value);
    }

    /// <summary>Заголовок месяца для панели.</summary>
    public string MonthTitle { get; } =
        DateTime.Today.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));

    public void Reload()
    {
        var summary = _reportService.Dashboard(DateTime.Today.Year, DateTime.Today.Month);

        PendingCount = summary.PendingCount;
        MonthVacationCount = summary.MonthVacationCount;
        ActiveEmployeeCount = summary.ActiveEmployeeCount;

        PendingRequests.Clear();
        foreach (var request in summary.PendingRequests)
        {
            PendingRequests.Add(new RequestRow
            {
                Id = request.Id,
                Number = $"№ {request.Number}",
                CreatedAt = request.CreatedAt.ToString("dd.MM.yyyy"),
                Employee = request.Employee?.FullName ?? string.Empty,
                Department = request.Employee?.Department?.Name ?? string.Empty,
                TypeName = request.VacationType?.Name ?? string.Empty,
                Period = $"{request.StartDate:dd.MM.yyyy} — {request.EndDate:dd.MM.yyyy}",
                Days = RequestRules.DaysCount(request.StartDate, request.EndDate),
                StatusName = RequestRules.NameOf(request.Status)
            });
        }

        MonthVacations.Clear();
        foreach (var record in summary.MonthVacations)
        {
            MonthVacations.Add(new MonthVacationRow
            {
                Employee = record.Employee?.FullName ?? string.Empty,
                Department = record.Employee?.Department?.Name ?? string.Empty,
                TypeName = record.VacationType?.Name ?? string.Empty,
                Period = $"{record.StartDate:dd.MM.yyyy} — {record.EndDate:dd.MM.yyyy}"
            });
        }

        DepartmentCounts.Clear();
        foreach (var count in summary.DepartmentCounts)
            DepartmentCounts.Add(count);
    }
}

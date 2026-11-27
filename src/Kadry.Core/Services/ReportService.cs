using Kadry.Core.Data;
using Kadry.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kadry.Core.Services;

/// <summary>Строка отчёта «Остатки дней».</summary>
public record RemainingRow(
    string Employee,
    string Department,
    string VacationType,
    int Limit,
    int Used,
    int Remaining);

/// <summary>Строка отчёта «Движение кадров».</summary>
public record MovementRow(
    string Employee,
    string Department,
    string Event,
    DateTime EventDate);

/// <summary>Сотрудники по отделам (для панели мониторинга).</summary>
public record DepartmentCount(string Department, int Count);

/// <summary>Сводка панели мониторинга.</summary>
public record DashboardSummary(
    int PendingCount,
    int MonthVacationCount,
    int ActiveEmployeeCount,
    IReadOnlyList<VacationRequest> PendingRequests,
    IReadOnlyList<VacationRecord> MonthVacations,
    IReadOnlyList<DepartmentCount> DepartmentCounts);

/// <summary>Отчётность: остатки дней, движение кадров, экспорт CSV, сводка для панели.</summary>
public class ReportService
{
    private readonly KadryContext _db;

    public ReportService(KadryContext db) => _db = db;

    /// <summary>Остатки дней по видам с лимитом за год; фильтр по отделу.</summary>
    public List<RemainingRow> RemainingDays(int year, int? departmentId = null)
    {
        var employees = _db.Employees
            .Include(e => e.Department)
            .Where(e => e.FiredAt == null);

        if (departmentId is int depId)
            employees = employees.Where(e => e.DepartmentId == depId);

        var types = _db.VacationTypes
            .Where(t => t.YearLimitDays > 0)
            .OrderBy(t => t.Name)
            .ToList();

        var records = _db.VacationRecords
            .Where(r => r.StartDate.Year == year)
            .ToList();

        var result = new List<RemainingRow>();
        foreach (var employee in employees.OrderBy(e => e.FullName).ToList())
        {
            foreach (var type in types)
            {
                var usedRecords = records
                    .Where(r => r.EmployeeId == employee.Id && r.VacationTypeId == type.Id)
                    .ToList();

                result.Add(new RemainingRow(
                    employee.FullName,
                    employee.Department?.Name ?? string.Empty,
                    type.Name,
                    type.YearLimitDays,
                    usedRecords.Sum(r => r.Days),
                    RequestRules.RemainingDays(type.YearLimitDays, usedRecords, year)));
            }
        }

        return result;
    }

    /// <summary>Движение кадров за период: приём и увольнение событиями.</summary>
    public List<MovementRow> Movements(DateTime from, DateTime to, int? departmentId = null)
    {
        var query = _db.Employees
            .Include(e => e.Department)
            .AsQueryable();

        if (departmentId is int depId)
            query = query.Where(e => e.DepartmentId == depId);

        var employees = query.ToList();
        var result = new List<MovementRow>();

        foreach (var employee in employees)
        {
            if (employee.HiredAt >= from.Date && employee.HiredAt <= to.Date)
            {
                result.Add(new MovementRow(
                    employee.FullName,
                    employee.Department?.Name ?? string.Empty,
                    "принят",
                    employee.HiredAt));
            }

            if (employee.FiredAt is DateTime fired && fired >= from.Date && fired <= to.Date)
            {
                result.Add(new MovementRow(
                    employee.FullName,
                    employee.Department?.Name ?? string.Empty,
                    "уволен",
                    fired));
            }
        }

        return result.OrderBy(r => r.EventDate).ThenBy(r => r.Employee).ToList();
    }

    /// <summary>Сводка панели: заявления в ожидании, отпуска месяца, сотрудники по отделам.</summary>
    public DashboardSummary Dashboard(int year, int month, int? departmentId = null)
    {
        var employees = _db.Employees.Where(e => e.FiredAt == null);
        if (departmentId is int depId)
            employees = employees.Where(e => e.DepartmentId == depId);

        var activeEmployeeCount = employees.Count();

        var departmentCounts = _db.Employees
            .Where(e => e.FiredAt == null)
            .Include(e => e.Department)
            .ToList()
            .GroupBy(e => e.Department?.Name ?? string.Empty)
            .Select(g => new DepartmentCount(g.Key, g.Count()))
            .OrderBy(d => d.Department)
            .ToList();

        var pending = _db.VacationRequests
            .Include(r => r.Employee)
            .ThenInclude(e => e!.Department)
            .Include(r => r.VacationType)
            .Where(r => r.Status == RequestStatus.New || r.Status == RequestStatus.Agreed);

        if (departmentId is int dep)
            pending = pending.Where(r => r.Employee!.DepartmentId == dep);

        var pendingList = pending
            .OrderBy(r => r.StartDate)
            .ThenBy(r => r.Number)
            .ToList();

        var monthRecords = _db.VacationRecords
            .Include(r => r.Employee)
            .ThenInclude(e => e!.Department)
            .Include(r => r.VacationType)
            .Where(r => r.StartDate.Year == year && r.StartDate.Month == month);

        if (departmentId is int depId2)
            monthRecords = monthRecords.Where(r => r.Employee!.DepartmentId == depId2);

        var monthList = monthRecords
            .OrderBy(r => r.StartDate)
            .ToList();

        return new DashboardSummary(
            pendingList.Count,
            monthList.Count,
            activeEmployeeCount,
            pendingList,
            monthList,
            departmentCounts);
    }

    // ---------- CSV ----------

    /// <summary>CSV отчёта «Остатки дней»: UTF-8 с BOM, разделитель «;».</summary>
    public static void WriteRemainingCsv(string path, IReadOnlyList<RemainingRow> rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Сотрудник;Отдел;Вид отпуска;Лимит;Использовано;Остаток");
        foreach (var row in rows)
            sb.AppendLine($"{row.Employee};{row.Department};{row.VacationType};{row.Limit};{row.Used};{row.Remaining}");

        File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(true));
    }

    /// <summary>CSV отчёта «Движение кадров»: UTF-8 с BOM, разделитель «;».</summary>
    public static void WriteMovementCsv(string path, IReadOnlyList<MovementRow> rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Сотрудник;Отдел;Событие;Дата");
        foreach (var row in rows)
            sb.AppendLine($"{row.Employee};{row.Department};{row.Event};{row.EventDate:dd.MM.yyyy}");

        File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(true));
    }
}

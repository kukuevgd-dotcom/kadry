using Kadry.Core.Data;
using Kadry.Core.Domain;
using Kadry.Core.Services;
using Xunit;

namespace Kadry.Core.Tests;

public class ReportServiceTests : IsolatedDbTests
{
    private Employee HireEmployee(KadryContext db, string name = "Иванов Иван Иванович",
                                  string number = "100", DateTime? firedAt = null,
                                  DateTime? hiredAt = null)
    {
        var employee = new Employee
        {
            FullName = name,
            PersonnelNumber = number,
            DepartmentId = db.Departments.First().Id,
            PositionId = db.Positions.First().Id,
            HiredAt = hiredAt ?? new DateTime(2026, 1, 10),
            FiredAt = firedAt
        };

        db.Employees.Add(employee);
        db.SaveChanges();
        return employee;
    }

    private VacationType MainType(KadryContext db) =>
        db.VacationTypes.Single(t => t.YearLimitDays == 28);

    private void AddRecord(KadryContext db, Employee employee, VacationType type,
                           DateTime start, DateTime end, int days)
    {
        var request = new VacationRequest
        {
            Number = db.VacationRequests.Count() + 1,
            EmployeeId = employee.Id,
            VacationTypeId = type.Id,
            StartDate = start,
            EndDate = end,
            Status = RequestStatus.Approved,
            CreatedAt = start.AddDays(-10)
        };
        db.VacationRequests.Add(request);
        db.SaveChanges();

        db.VacationRecords.Add(new VacationRecord
        {
            RequestId = request.Id,
            EmployeeId = employee.Id,
            VacationTypeId = type.Id,
            StartDate = start,
            EndDate = end,
            Days = days
        });
        db.SaveChanges();
    }

    [Fact]
    public void RemainingDays_ComputesUsedAndRemaining()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var type = MainType(db);
        AddRecord(db, employee, type, new DateTime(2026, 3, 1), new DateTime(2026, 3, 10), 10);

        var rows = new ReportService(db).RemainingDays(2026);

        // строка на каждый вид с лимитом: основной (28) и дополнительный (7)
        var main = rows.Single(r => r.Limit == 28);
        Assert.Equal(10, main.Used);
        Assert.Equal(18, main.Remaining);

        var additional = rows.Single(r => r.Limit == 7);
        Assert.Equal(0, additional.Used);
        Assert.Equal(7, additional.Remaining);
    }

    [Fact]
    public void RemainingDays_FiltersByDepartment()
    {
        using var db = NewDb();
        var directory = new DirectoryService(db);
        var warehouse = directory.AddDepartment("Склад");

        var dev = HireEmployee(db);
        db.Employees.Add(new Employee
        {
            FullName = "Петров Пётр Петрович",
            PersonnelNumber = "101",
            DepartmentId = warehouse.Id,
            PositionId = db.Positions.First().Id,
            HiredAt = new DateTime(2026, 1, 10)
        });
        db.SaveChanges();

        var rows = new ReportService(db).RemainingDays(2026, warehouse.Id);

        Assert.All(rows, r => Assert.Equal("Склад", r.Department));
        Assert.DoesNotContain(rows, r => r.Department == dev.Department?.Name);
    }

    [Fact]
    public void Movements_IncludesHiredAndFiredEventsInPeriod()
    {
        using var db = NewDb();
        HireEmployee(db, "Иванов Иван Иванович", "100", new DateTime(2026, 10, 5),
            hiredAt: new DateTime(2026, 10, 1));

        var movements = new ReportService(db).Movements(
            new DateTime(2026, 10, 1), new DateTime(2026, 10, 31));

        Assert.Equal(2, movements.Count);
        Assert.Contains(movements, m => m.Event == "принят");
        Assert.Contains(movements, m => m.Event == "уволен");
    }

    [Fact]
    public void Movements_ExcludesEventsOutsidePeriod()
    {
        using var db = NewDb();
        HireEmployee(db);

        var movements = new ReportService(db).Movements(
            new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        Assert.Empty(movements);
    }

    [Fact]
    public void WriteRemainingCsv_CreatesFileWithHeaderAndRows()
    {
        var path = Path.Combine(Path.GetTempPath(), $"kadry_csv_{Guid.NewGuid():N}.csv");
        try
        {
            ReportService.WriteRemainingCsv(path, new[]
            {
                new RemainingRow("Иванов Иван Иванович", "Отдел разработки",
                    "Основной ежегодный оплачиваемый", 28, 10, 18)
            });

            var text = File.ReadAllText(path);
            Assert.Contains("Сотрудник;Отдел;Вид отпуска;Лимит;Использовано;Остаток", text);
            Assert.Contains("Иванов Иван Иванович;Отдел разработки", text);
            Assert.Contains(";28;10;18", text);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Dashboard_CountsPendingAndMonthVacations()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var type = MainType(db);
        AddRecord(db, employee, type, new DateTime(2026, 11, 2), new DateTime(2026, 11, 8), 7);

        var service = new VacationRequestService(db);
        var pending = service.Register(employee.Id, type.Id,
            new DateTime(2026, 12, 1), new DateTime(2026, 12, 5));
        service.Agree(pending.Id);
        service.Register(employee.Id, type.Id,
            new DateTime(2026, 12, 10), new DateTime(2026, 12, 15));

        var summary = new ReportService(db).Dashboard(2026, 11);

        Assert.Equal(2, summary.PendingCount);
        Assert.Equal(1, summary.MonthVacationCount);
        Assert.Equal(1, summary.ActiveEmployeeCount);
        Assert.Equal(2, summary.PendingRequests.Count);
        Assert.Single(summary.MonthVacations);
    }
}

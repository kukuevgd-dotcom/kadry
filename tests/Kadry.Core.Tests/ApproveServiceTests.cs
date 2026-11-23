using Kadry.Core.Data;
using Kadry.Core.Domain;
using Kadry.Core.Services;
using Xunit;

namespace Kadry.Core.Tests;

public class ApproveServiceTests : IsolatedDbTests
{
    private Employee HireEmployee(KadryContext db, string number = "100")
    {
        var service = new EmployeeService(db);
        return service.Hire(new Employee
        {
            FullName = "Иванов Иван Иванович",
            PersonnelNumber = number,
            DepartmentId = db.Departments.First().Id,
            PositionId = db.Positions.First().Id,
            HiredAt = new DateTime(2026, 1, 10)
        });
    }

    private VacationType MainType(KadryContext db) =>
        db.VacationTypes.Single(t => t.YearLimitDays == 28);

    /// <summary>Создаёт уже согласованное заявление.</summary>
    private VacationRequest RegisterAndAgree(VacationRequestService service, Employee employee,
                                              VacationType type, DateTime start, DateTime end)
    {
        var request = service.Register(employee.Id, type.Id, start, end);
        service.Agree(request.Id);
        return request;
    }

    [Fact]
    public void Approve_CreatesRecordAndChangesStatus()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var type = MainType(db);
        var service = new VacationRequestService(db);
        var request = RegisterAndAgree(service, employee, type,
            new DateTime(2026, 6, 1), new DateTime(2026, 6, 10));

        service.Approve(request.Id);

        var stored = db.VacationRequests.Single(r => r.Id == request.Id);
        Assert.Equal(RequestStatus.Approved, stored.Status);

        var record = db.VacationRecords.Single();
        Assert.Equal(request.Id, record.RequestId);
        Assert.Equal(employee.Id, record.EmployeeId);
        Assert.Equal(10, record.Days);
    }

    [Fact]
    public void Approve_FromNew_Throws()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var type = MainType(db);
        var service = new VacationRequestService(db);
        var request = service.Register(employee.Id, type.Id,
            new DateTime(2026, 6, 1), new DateTime(2026, 6, 10));

        // несогласованное утвердить нельзя
        Assert.Throws<InvalidOperationException>(() => service.Approve(request.Id));
    }

    [Fact]
    public void Approve_Twice_Throws()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var type = MainType(db);
        var service = new VacationRequestService(db);
        var request = RegisterAndAgree(service, employee, type,
            new DateTime(2026, 6, 1), new DateTime(2026, 6, 10));
        service.Approve(request.Id);

        Assert.Throws<InvalidOperationException>(() => service.Approve(request.Id));
    }

    [Fact]
    public void Approve_RemainingShrunkAfterAgreement_Throws()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var type = MainType(db);
        var service = new VacationRequestService(db);

        // первое заявление: согласовано, ждёт утверждения
        var pending = RegisterAndAgree(service, employee, type,
            new DateTime(2026, 7, 1), new DateTime(2026, 7, 14));

        // параллельно другое заявление согласовано и утверждено раньше
        var other = RegisterAndAgree(service, employee, type,
            new DateTime(2026, 3, 1), new DateTime(2026, 3, 20));
        service.Approve(other.Id); // 20 дней из 28

        // у pending: 14 дней, остаток 8 — утверждить нельзя
        Assert.Throws<InvalidOperationException>(() => service.Approve(pending.Id));
    }

    [Fact]
    public void Approve_OverlapAppearedAfterAgreement_Throws()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var type = MainType(db);
        var service = new VacationRequestService(db);

        // первое: согласовано, ждёт утверждения (10–20 июня, 11 дней)
        var pending = RegisterAndAgree(service, employee, type,
            new DateTime(2026, 6, 10), new DateTime(2026, 6, 20));

        // параллельное заявление регистрируется, пока pending не утверждено:
        // пересечения с утверждёнными ещё нет, Register разрешает
        var parallel = service.Register(employee.Id, type.Id,
            new DateTime(2026, 6, 15), new DateTime(2026, 6, 18));
        service.Agree(parallel.Id);
        service.Approve(parallel.Id); // запись 15–18 июня появилась

        // теперь утверждение pending должно упасть: пересечение 15–18 июня
        Assert.Throws<InvalidOperationException>(() => service.Approve(pending.Id));
        Assert.Single(db.VacationRecords);
    }

    [Fact]
    public void RecordsForYear_ReturnsOnlyMatchingYear()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var type = MainType(db);
        var service = new VacationRequestService(db);

        var first = RegisterAndAgree(service, employee, type,
            new DateTime(2026, 6, 1), new DateTime(2026, 6, 10));
        service.Approve(first.Id);

        var second = RegisterAndAgree(service, employee, type,
            new DateTime(2027, 1, 10), new DateTime(2027, 1, 20));
        service.Approve(second.Id);

        Assert.Single(service.RecordsForYear(2026));
        Assert.Single(service.RecordsForYear(2027));
        Assert.Empty(service.RecordsForYear(2025));
    }

    [Fact]
    public void RecordsForYear_FiltersByDepartment()
    {
        using var db = NewDb();
        var directory = new DirectoryService(db);
        var warehouse = directory.AddDepartment("Склад");
        var employeeService = new EmployeeService(db);

        var dev = employeeService.Hire(new Employee
        {
            FullName = "Иванов Иван Иванович",
            PersonnelNumber = "100",
            DepartmentId = db.Departments.First().Id,
            PositionId = db.Positions.First().Id,
            HiredAt = new DateTime(2026, 1, 10)
        });
        var storekeeper = employeeService.Hire(new Employee
        {
            FullName = "Петров Пётр Петрович",
            PersonnelNumber = "101",
            DepartmentId = warehouse.Id,
            PositionId = db.Positions.First().Id,
            HiredAt = new DateTime(2026, 1, 10)
        });

        var type = MainType(db);
        var service = new VacationRequestService(db);

        foreach (var employee in new[] { dev, storekeeper })
        {
            var request = RegisterAndAgree(service, employee, type,
                new DateTime(2026, 6, 1), new DateTime(2026, 6, 10));
            service.Approve(request.Id);
        }

        Assert.Single(service.RecordsForYear(2026, db.Departments.First().Id));
        Assert.Single(service.RecordsForYear(2026, warehouse.Id));
        Assert.Equal(2, service.RecordsForYear(2026).Count);
    }
}

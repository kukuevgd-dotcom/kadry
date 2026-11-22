using Kadry.Core.Data;
using Kadry.Core.Domain;
using Kadry.Core.Services;
using Xunit;

namespace Kadry.Core.Tests;

public class VacationRequestServiceTests : IsolatedDbTests
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

    [Fact]
    public void Register_AssignsSequentialNumbers()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var type = MainType(db);
        var service = new VacationRequestService(db);

        var first = service.Register(employee.Id, type.Id,
            new DateTime(2026, 6, 1), new DateTime(2026, 6, 10));
        var second = service.Register(employee.Id, type.Id,
            new DateTime(2026, 8, 1), new DateTime(2026, 8, 5));

        Assert.Equal(1, first.Number);
        Assert.Equal(2, second.Number);
        Assert.Equal(RequestStatus.New, second.Status);
        Assert.Equal(DateTime.Today, second.CreatedAt);
    }

    [Fact]
    public void Register_EndBeforeStart_Throws()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var service = new VacationRequestService(db);

        Assert.Throws<ArgumentException>(() =>
            service.Register(employee.Id, MainType(db).Id,
                new DateTime(2026, 6, 10), new DateTime(2026, 6, 1)));
    }

    [Fact]
    public void Register_OverlapsApprovedVacation_Throws()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var type = MainType(db);
        var service = new VacationRequestService(db);

        var existing = service.Register(employee.Id, type.Id,
            new DateTime(2026, 6, 10), new DateTime(2026, 6, 20));
        db.VacationRecords.Add(new VacationRecord
        {
            RequestId = existing.Id,
            EmployeeId = employee.Id,
            VacationTypeId = type.Id,
            StartDate = new DateTime(2026, 6, 10),
            EndDate = new DateTime(2026, 6, 20),
            Days = 11
        });
        db.SaveChanges();

        Assert.Throws<InvalidOperationException>(() =>
            service.Register(employee.Id, type.Id,
                new DateTime(2026, 6, 15), new DateTime(2026, 6, 18)));
    }

    [Fact]
    public void Register_FiredEmployee_Throws()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        new EmployeeService(db).Fire(employee.Id, new DateTime(2026, 11, 1));
        var service = new VacationRequestService(db);

        Assert.Throws<InvalidOperationException>(() =>
            service.Register(employee.Id, MainType(db).Id,
                new DateTime(2026, 12, 1), new DateTime(2026, 12, 5)));
    }

    [Fact]
    public void Agree_InsufficientRemaining_Throws()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var type = MainType(db);
        var service = new VacationRequestService(db);

        var spent = service.Register(employee.Id, type.Id,
            new DateTime(2026, 3, 1), new DateTime(2026, 3, 20));
        db.VacationRecords.Add(new VacationRecord
        {
            RequestId = spent.Id,
            EmployeeId = employee.Id,
            VacationTypeId = type.Id,
            StartDate = new DateTime(2026, 3, 1),
            EndDate = new DateTime(2026, 3, 20),
            Days = 20
        });
        db.SaveChanges();

        var request = service.Register(employee.Id, type.Id,
            new DateTime(2026, 7, 1), new DateTime(2026, 7, 10));

        Assert.Throws<InvalidOperationException>(() => service.Agree(request.Id));
    }

    [Fact]
    public void Agree_EnoughRemaining_SetsStatus()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var type = MainType(db);
        var service = new VacationRequestService(db);

        var spent = service.Register(employee.Id, type.Id,
            new DateTime(2026, 3, 1), new DateTime(2026, 3, 10));
        db.VacationRecords.Add(new VacationRecord
        {
            RequestId = spent.Id,
            EmployeeId = employee.Id,
            VacationTypeId = type.Id,
            StartDate = new DateTime(2026, 3, 1),
            EndDate = new DateTime(2026, 3, 10),
            Days = 10
        });
        db.SaveChanges();

        var request = service.Register(employee.Id, type.Id,
            new DateTime(2026, 7, 1), new DateTime(2026, 7, 10));

        service.Agree(request.Id);

        Assert.Equal(RequestStatus.Agreed, db.VacationRequests.Single(r => r.Id == request.Id).Status);
    }

    [Fact]
    public void Agree_UnlimitedType_SkipsRemainderCheck()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var unpaid = db.VacationTypes.Single(t => t.IsUnpaid);
        var service = new VacationRequestService(db);

        var request = service.Register(employee.Id, unpaid.Id,
            new DateTime(2026, 7, 1), new DateTime(2026, 7, 30));

        service.Agree(request.Id);

        Assert.Equal(RequestStatus.Agreed, db.VacationRequests.Single().Status);
    }

    [Fact]
    public void Refuse_StoresReasonAndClosesRequest()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var service = new VacationRequestService(db);
        var request = service.Register(employee.Id, MainType(db).Id,
            new DateTime(2026, 7, 1), new DateTime(2026, 7, 10));

        service.Refuse(request.Id, "Отпуск в июле не согласован: дедлайны проекта");

        var stored = db.VacationRequests.Single();
        Assert.Equal(RequestStatus.Refused, stored.Status);
        Assert.Contains("дедлайны", stored.RefuseReason);

        // терминальный статус: повторное отклонение запрещено
        Assert.Throws<InvalidOperationException>(() =>
            service.Refuse(request.Id, "ещё раз"));
    }

    [Fact]
    public void Refuse_WithoutReason_Throws()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var service = new VacationRequestService(db);
        var request = service.Register(employee.Id, MainType(db).Id,
            new DateTime(2026, 7, 1), new DateTime(2026, 7, 10));

        Assert.Throws<ArgumentException>(() => service.Refuse(request.Id, "  "));
    }

    [Fact]
    public void Agree_TerminalStatus_Throws()
    {
        using var db = NewDb();
        var employee = HireEmployee(db);
        var service = new VacationRequestService(db);
        var request = service.Register(employee.Id, MainType(db).Id,
            new DateTime(2026, 7, 1), new DateTime(2026, 7, 10));
        service.Refuse(request.Id, "нет");

        Assert.Throws<InvalidOperationException>(() => service.Agree(request.Id));
    }

    [Fact]
    public void List_FiltersByStatusAndSearch()
    {
        using var db = NewDb();
        var ivanov = HireEmployee(db, "100");
        var petrov = new EmployeeService(db).Hire(new Employee
        {
            FullName = "Петров Пётр Петрович",
            PersonnelNumber = "101",
            DepartmentId = db.Departments.First().Id,
            PositionId = db.Positions.First().Id,
            HiredAt = new DateTime(2026, 1, 10)
        });
        var type = MainType(db);
        var service = new VacationRequestService(db);

        var first = service.Register(ivanov.Id, type.Id,
            new DateTime(2026, 6, 1), new DateTime(2026, 6, 10));
        service.Register(petrov.Id, type.Id,
            new DateTime(2026, 8, 1), new DateTime(2026, 8, 5));
        service.Agree(first.Id);

        Assert.Equal(2, service.List().Count);
        Assert.Single(service.List(status: RequestStatus.New));
        Assert.Single(service.List(status: RequestStatus.Agreed));
        Assert.Single(service.List(search: "петров"));
    }
}

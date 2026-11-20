using Kadry.Core.Data;
using Kadry.Core.Domain;
using Kadry.Core.Services;
using Xunit;

namespace Kadry.Core.Tests;

public class EmployeeServiceTests : IsolatedDbTests
{
    private static Employee Sample(KadryContext db, string number = "001", string name = "Иванов Иван Иванович") => new()
    {
        FullName = name,
        PersonnelNumber = number,
        DepartmentId = db.Departments.First().Id,
        PositionId = db.Positions.First().Id,
        HiredAt = new DateTime(2026, 1, 15)
    };

    [Fact]
    public void Hire_ValidEmployee_SavesAndReturns()
    {
        using var db = NewDb();
        var service = new EmployeeService(db);

        var employee = service.Hire(Sample(db));

        Assert.True(employee.Id > 0);
        Assert.Equal("Иванов Иван Иванович", db.Employees.Single().FullName);
    }

    [Fact]
    public void Hire_DuplicatePersonnelNumber_Throws()
    {
        using var db = NewDb();
        var service = new EmployeeService(db);
        service.Hire(Sample(db, "001"));

        Assert.Throws<InvalidOperationException>(() => service.Hire(Sample(db, "001", "Петров Пётр Петрович")));
    }

    [Fact]
    public void Hire_EmptyName_Throws()
    {
        using var db = NewDb();
        var service = new EmployeeService(db);

        Assert.Throws<ArgumentException>(() => service.Hire(Sample(db, name: " ")));
    }

    [Fact]
    public void Update_ChangesFieldsAndKeepsId()
    {
        using var db = NewDb();
        var service = new EmployeeService(db);
        var employee = service.Hire(Sample(db));

        var edited = new Employee
        {
            Id = employee.Id,
            FullName = "Иванов Иван Иванович-мл.",
            PersonnelNumber = "002",
            DepartmentId = employee.DepartmentId,
            PositionId = employee.PositionId,
            HiredAt = employee.HiredAt
        };
        service.Update(edited);

        var stored = db.Employees.Single();
        Assert.Equal("Иванов Иван Иванович-мл.", stored.FullName);
        Assert.Equal("002", stored.PersonnelNumber);
    }

    [Fact]
    public void Fire_SetsDate()
    {
        using var db = NewDb();
        var service = new EmployeeService(db);
        var employee = service.Hire(Sample(db));

        service.Fire(employee.Id, new DateTime(2026, 11, 20));

        Assert.NotNull(db.Employees.Single().FiredAt);
    }

    [Fact]
    public void Fire_Twice_Throws()
    {
        using var db = NewDb();
        var service = new EmployeeService(db);
        var employee = service.Hire(Sample(db));
        service.Fire(employee.Id, new DateTime(2026, 11, 20));

        Assert.Throws<InvalidOperationException>(() => service.Fire(employee.Id, new DateTime(2026, 12, 1)));
    }

    [Fact]
    public void Fire_BeforeHireDate_Throws()
    {
        using var db = NewDb();
        var service = new EmployeeService(db);
        var employee = service.Hire(Sample(db));

        Assert.Throws<InvalidOperationException>(() => service.Fire(employee.Id, new DateTime(2025, 1, 1)));
    }

    [Fact]
    public void Delete_WithRequests_Throws()
    {
        using var db = NewDb();
        var service = new EmployeeService(db);
        var employee = service.Hire(Sample(db));

        db.VacationRequests.Add(new VacationRequest
        {
            Number = 1,
            EmployeeId = employee.Id,
            VacationTypeId = db.VacationTypes.First().Id,
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 10),
            Status = RequestStatus.New,
            CreatedAt = new DateTime(2026, 5, 20)
        });
        db.SaveChanges();

        Assert.Throws<InvalidOperationException>(() => service.Delete(employee.Id));
    }

    [Fact]
    public void Delete_WithoutRequests_Removes()
    {
        using var db = NewDb();
        var service = new EmployeeService(db);
        var employee = service.Hire(Sample(db));

        service.Delete(employee.Id);

        Assert.Empty(db.Employees);
    }

    [Fact]
    public void List_FiltersBySearchAndStatus()
    {
        using var db = NewDb();
        var service = new EmployeeService(db);
        var first = service.Hire(Sample(db, "001", "Иванов Иван Иванович"));
        service.Hire(Sample(db, "002", "Петров Пётр Петрович"));
        service.Fire(first.Id, new DateTime(2026, 11, 20));

        Assert.Single(service.List(search: "петров"));
        Assert.Single(service.List(onlyActive: false));
        Assert.Single(service.List(onlyActive: true));
        Assert.Equal(2, service.List().Count);
    }
}

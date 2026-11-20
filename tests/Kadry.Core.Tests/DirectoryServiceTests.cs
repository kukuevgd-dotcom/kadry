using Kadry.Core.Services;
using Xunit;

namespace Kadry.Core.Tests;

public class DirectoryServiceTests : IsolatedDbTests
{
    [Fact]
    public void AddDepartment_Duplicate_Throws()
    {
        using var db = NewDb();
        var service = new DirectoryService(db);

        Assert.Throws<InvalidOperationException>(() => service.AddDepartment("Бухгалтерия"));
    }

    [Fact]
    public void AddDepartment_EmptyName_Throws()
    {
        using var db = NewDb();
        var service = new DirectoryService(db);

        Assert.Throws<ArgumentException>(() => service.AddDepartment("  "));
    }

    [Fact]
    public void DeleteDepartment_WithEmployees_Throws()
    {
        using var db = NewDb();
        var service = new DirectoryService(db);
        var employeeService = new EmployeeService(db);
        var department = service.AddDepartment("Склад");
        employeeService.Hire(new Kadry.Core.Domain.Employee
        {
            FullName = "Иванов Иван Иванович",
            PersonnelNumber = "100",
            DepartmentId = department.Id,
            PositionId = db.Positions.First().Id,
            HiredAt = new DateTime(2026, 1, 10)
        });

        Assert.Throws<InvalidOperationException>(() => service.DeleteDepartment(department.Id));
    }

    [Fact]
    public void DeleteDepartment_Empty_Removes()
    {
        using var db = NewDb();
        var service = new DirectoryService(db);
        int id = service.AddDepartment("Склад").Id;

        service.DeleteDepartment(id);

        Assert.DoesNotContain(db.Departments, d => d.Name == "Склад");
    }

    [Fact]
    public void RenamePosition_ChangesName()
    {
        using var db = NewDb();
        var service = new DirectoryService(db);
        var position = service.AddPosition("Аналитик");

        service.RenamePosition(position.Id, "Системный аналитик");

        Assert.Equal("Системный аналитик", db.Positions.Single(p => p.Id == position.Id).Name);
    }

    [Fact]
    public void AddVacationType_WithNegativeLimit_Throws()
    {
        using var db = NewDb();
        var service = new DirectoryService(db);

        Assert.Throws<ArgumentException>(() => service.AddVacationType("Творческий", -5, false));
    }

    [Fact]
    public void UpdateVacationType_ChangesFields()
    {
        using var db = NewDb();
        var service = new DirectoryService(db);
        var type = service.AddVacationType("Дополнительный северный", 14, false);

        service.UpdateVacationType(type.Id, "Дополнительный (северный)", 16, false);

        var stored = db.VacationTypes.Single(t => t.Id == type.Id);
        Assert.Equal(16, stored.YearLimitDays);
        Assert.Equal("Дополнительный (северный)", stored.Name);
    }

    [Fact]
    public void DeleteVacationType_UsedInRequests_Throws()
    {
        using var db = NewDb();
        var service = new DirectoryService(db);
        var type = service.AddVacationType("Творческий", 10, false);
        var employeeService = new EmployeeService(db);
        var employee = employeeService.Hire(new Kadry.Core.Domain.Employee
        {
            FullName = "Иванов Иван Иванович",
            PersonnelNumber = "100",
            DepartmentId = db.Departments.First().Id,
            PositionId = db.Positions.First().Id,
            HiredAt = new DateTime(2026, 1, 10)
        });

        db.VacationRequests.Add(new Kadry.Core.Domain.VacationRequest
        {
            Number = 1,
            EmployeeId = employee.Id,
            VacationTypeId = type.Id,
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 5),
            Status = Kadry.Core.Domain.RequestStatus.New,
            CreatedAt = new DateTime(2026, 6, 28)
        });
        db.SaveChanges();

        Assert.Throws<InvalidOperationException>(() => service.DeleteVacationType(type.Id));
    }
}

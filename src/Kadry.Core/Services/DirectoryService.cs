using Kadry.Core.Data;
using Kadry.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kadry.Core.Services;

/// <summary>Справочники: отделы, должности, виды отпусков.</summary>
public class DirectoryService
{
    private readonly KadryContext _db;

    public DirectoryService(KadryContext db) => _db = db;

    // ---------- Отделы ----------

    public List<Department> Departments() => _db.Departments.OrderBy(d => d.Name).ToList();

    public Department AddDepartment(string name)
    {
        name = RequireName(name, "Название отдела");
        EnsureNameFree(_db.Departments, name, null, "Отдел с таким названием уже существует.");

        var department = new Department { Name = name };
        _db.Departments.Add(department);
        _db.SaveChanges();
        return department;
    }

    public void RenameDepartment(int id, string name)
    {
        name = RequireName(name, "Название отдела");
        var department = _db.Departments.Find(id)
            ?? throw new InvalidOperationException("Отдел не найден.");
        EnsureNameFree(_db.Departments, name, id, "Отдел с таким названием уже существует.");

        department.Name = name;
        _db.SaveChanges();
    }

    public void DeleteDepartment(int id)
    {
        var department = _db.Departments.Find(id)
            ?? throw new InvalidOperationException("Отдел не найден.");

        if (_db.Employees.Any(e => e.DepartmentId == id))
            throw new InvalidOperationException("В отделе есть сотрудники — удаление запрещено.");

        _db.Departments.Remove(department);
        _db.SaveChanges();
    }

    // ---------- Должности ----------

    public List<Position> Positions() => _db.Positions.OrderBy(p => p.Name).ToList();

    public Position AddPosition(string name)
    {
        name = RequireName(name, "Название должности");
        EnsureNameFree(_db.Positions, name, null, "Должность с таким названием уже существует.");

        var position = new Position { Name = name };
        _db.Positions.Add(position);
        _db.SaveChanges();
        return position;
    }

    public void RenamePosition(int id, string name)
    {
        name = RequireName(name, "Название должности");
        var position = _db.Positions.Find(id)
            ?? throw new InvalidOperationException("Должность не найдена.");
        EnsureNameFree(_db.Positions, name, id, "Должность с таким названием уже существует.");

        position.Name = name;
        _db.SaveChanges();
    }

    public void DeletePosition(int id)
    {
        var position = _db.Positions.Find(id)
            ?? throw new InvalidOperationException("Должность не найдена.");

        if (_db.Employees.Any(e => e.PositionId == id))
            throw new InvalidOperationException("На должности есть сотрудники — удаление запрещено.");

        _db.Positions.Remove(position);
        _db.SaveChanges();
    }

    // ---------- Виды отпусков ----------

    public List<VacationType> VacationTypes() => _db.VacationTypes.OrderBy(t => t.Name).ToList();

    public VacationType AddVacationType(string name, int yearLimitDays, bool isUnpaid)
    {
        (name, yearLimitDays) = ValidateVacationType(name, yearLimitDays);
        EnsureNameFree(_db.VacationTypes, name, null, "Вид отпуска с таким названием уже существует.");

        var type = new VacationType { Name = name, YearLimitDays = yearLimitDays, IsUnpaid = isUnpaid };
        _db.VacationTypes.Add(type);
        _db.SaveChanges();
        return type;
    }

    public void UpdateVacationType(int id, string name, int yearLimitDays, bool isUnpaid)
    {
        (name, yearLimitDays) = ValidateVacationType(name, yearLimitDays);
        var type = _db.VacationTypes.Find(id)
            ?? throw new InvalidOperationException("Вид отпуска не найден.");
        EnsureNameFree(_db.VacationTypes, name, id, "Вид отпуска с таким названием уже существует.");

        type.Name = name;
        type.YearLimitDays = yearLimitDays;
        type.IsUnpaid = isUnpaid;
        _db.SaveChanges();
    }

    public void DeleteVacationType(int id)
    {
        var type = _db.VacationTypes.Find(id)
            ?? throw new InvalidOperationException("Вид отпуска не найден.");

        bool referenced = _db.VacationRequests.Any(r => r.VacationTypeId == id)
                       || _db.VacationRecords.Any(r => r.VacationTypeId == id);
        if (referenced)
            throw new InvalidOperationException(
                "Вид отпуска используется в заявлениях — удаление запрещено.");

        _db.VacationTypes.Remove(type);
        _db.SaveChanges();
    }

    // ---------- Вспомогательное ----------

    private static string RequireName(string name, string field)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            throw new ArgumentException($"{field} не заполнено.");
        return name;
    }

    private static (string, int) ValidateVacationType(string name, int yearLimitDays)
    {
        name = RequireName(name, "Название вида отпуска");
        if (yearLimitDays < 0)
            throw new ArgumentException("Лимит дней не может быть отрицательным.");
        return (name, yearLimitDays);
    }

    private static void EnsureNameFree<T>(IQueryable<T> set, string name, int? exceptId, string message)
        where T : class, Kadry.Core.Domain.IHasId, Kadry.Core.Domain.IHasName
    {
        bool taken = exceptId is null
            ? set.Any(x => x.Name == name)
            : set.Any(x => x.Name == name && x.Id != exceptId);

        if (taken)
            throw new InvalidOperationException(message);
    }
}

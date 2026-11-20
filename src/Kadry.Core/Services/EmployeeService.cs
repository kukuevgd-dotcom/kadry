using Kadry.Core.Data;
using Kadry.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kadry.Core.Services;

/// <summary>Операции со списком сотрудников: приём, изменение, увольнение, удаление.</summary>
public class EmployeeService
{
    private readonly KadryContext _db;

    public EmployeeService(KadryContext db) => _db = db;

    /// <summary>Список сотрудников с поиском и фильтрами.</summary>
    public List<Employee> List(string search = "", int? departmentId = null, bool? onlyActive = null)
    {
        var query = _db.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .AsQueryable();

        if (departmentId is int depId)
            query = query.Where(e => e.DepartmentId == depId);

        if (onlyActive == true)
            query = query.Where(e => e.FiredAt == null);
        else if (onlyActive == false)
            query = query.Where(e => e.FiredAt != null);

        // Поиск по ФИО выполняется в памяти: SQLite LIKE нечувствителен к регистру
        // только для латиницы, а искать нужно и по русским фамилиям в любом регистре.
        IEnumerable<Employee> ordered = query.OrderBy(e => e.FullName).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string s = search.Trim();
            ordered = ordered.Where(e =>
                e.FullName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                e.PersonnelNumber.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        return ordered.ToList();
    }

    public Employee? Find(int id) =>
        _db.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefault(e => e.Id == id);

    /// <summary>Приём нового сотрудника. Возвращает сохранённую запись.</summary>
    public Employee Hire(Employee employee)
    {
        Validate(employee);
        EnsureReferencesExist(employee);
        EnsurePersonnelNumberFree(employee.PersonnelNumber, exceptId: null);

        employee.Id = 0;
        _db.Employees.Add(employee);
        _db.SaveChanges();
        return employee;
    }

    public void Update(Employee employee)
    {
        Validate(employee);
        EnsureReferencesExist(employee);
        EnsurePersonnelNumberFree(employee.PersonnelNumber, exceptId: employee.Id);

        var existing = _db.Employees.Find(employee.Id)
            ?? throw new InvalidOperationException("Сотрудник не найден (возможно, запись удалена).");

        existing.FullName = employee.FullName;
        existing.PersonnelNumber = employee.PersonnelNumber;
        existing.DepartmentId = employee.DepartmentId;
        existing.PositionId = employee.PositionId;
        existing.HiredAt = employee.HiredAt;
        _db.SaveChanges();
    }

    /// <summary>Увольнение: сотрудник скрывается из списка работающих, история сохраняется.</summary>
    public void Fire(int employeeId, DateTime fireDate)
    {
        var employee = _db.Employees.Find(employeeId)
            ?? throw new InvalidOperationException("Сотрудник не найден.");

        if (employee.FiredAt is not null)
            throw new InvalidOperationException("Сотрудник уже уволен.");

        if (fireDate.Date < employee.HiredAt.Date)
            throw new InvalidOperationException("Дата увольнения не может быть раньше даты приёма.");

        employee.FiredAt = fireDate.Date;
        _db.SaveChanges();
    }

    /// <summary>Удаление допустимо только без истории заявлений.</summary>
    public void Delete(int employeeId)
    {
        var employee = _db.Employees.Find(employeeId)
            ?? throw new InvalidOperationException("Сотрудник не найден.");

        if (_db.VacationRequests.Any(r => r.EmployeeId == employeeId))
            throw new InvalidOperationException(
                "У сотрудника есть заявления на отпуск — удаление запрещено. " +
                "Используйте увольнение: история сохранится.");

        _db.Employees.Remove(employee);
        _db.SaveChanges();
    }

    private static void Validate(Employee e)
    {
        if (string.IsNullOrWhiteSpace(e.FullName))
            throw new ArgumentException("ФИО не заполнено.");
        if (string.IsNullOrWhiteSpace(e.PersonnelNumber))
            throw new ArgumentException("Табельный номер не заполнен.");
        if (e.DepartmentId <= 0)
            throw new ArgumentException("Не выбран отдел.");
        if (e.PositionId <= 0)
            throw new ArgumentException("Не выбрана должность.");
        if (e.HiredAt == default)
            throw new ArgumentException("Не указана дата приёма.");
    }

    private void EnsureReferencesExist(Employee e)
    {
        if (!_db.Departments.Any(d => d.Id == e.DepartmentId))
            throw new ArgumentException("Выбранный отдел не существует.");
        if (!_db.Positions.Any(p => p.Id == e.PositionId))
            throw new ArgumentException("Выбранная должность не существует.");
    }

    private void EnsurePersonnelNumberFree(string personnelNumber, int? exceptId)
    {
        bool taken = _db.Employees.Any(e =>
            e.PersonnelNumber == personnelNumber &&
            (exceptId == null || e.Id != exceptId));

        if (taken)
            throw new InvalidOperationException($"Табельный номер {personnelNumber} уже занят.");
    }
}

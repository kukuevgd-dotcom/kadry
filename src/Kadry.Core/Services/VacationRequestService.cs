using Kadry.Core.Data;
using Kadry.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kadry.Core.Services;

/// <summary>Жизненный цикл заявления: регистрация, согласование, отклонение.</summary>
public class VacationRequestService
{
    private readonly KadryContext _db;

    public VacationRequestService(KadryContext db) => _db = db;

    /// <summary>Журнал заявлений с фильтрами; поиск по ФИО — в памяти (кириллица).</summary>
    public List<VacationRequest> List(RequestStatus? status = null, int? employeeId = null,
                                      int? vacationTypeId = null, string search = "")
    {
        var query = _db.VacationRequests
            .Include(r => r.Employee)
            .ThenInclude(e => e!.Department)
            .Include(r => r.VacationType)
            .AsQueryable();

        if (status is RequestStatus st)
            query = query.Where(r => r.Status == st);
        if (employeeId is int eid)
            query = query.Where(r => r.EmployeeId == eid);
        if (vacationTypeId is int tid)
            query = query.Where(r => r.VacationTypeId == tid);

        var result = query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Number)
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string s = search.Trim();
            result = result.Where(r =>
                r.Employee!.FullName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                r.Number.ToString() == s)
                .ToList();
        }

        return result;
    }

    /// <summary>Регистрация заявления: сквозной номер, статус «Новое».</summary>
    public VacationRequest Register(int employeeId, int vacationTypeId, DateTime start, DateTime end)
    {
        var employee = _db.Employees.Find(employeeId)
            ?? throw new InvalidOperationException("Сотрудник не найден.");
        if (!employee.IsActive)
            throw new InvalidOperationException("Сотрудник уволен — заявление оформить нельзя.");

        var type = _db.VacationTypes.Find(vacationTypeId)
            ?? throw new InvalidOperationException("Вид отпуска не найден.");

        // DaysCount проверяет порядок дат и вернёт число календарных дней
        _ = RequestRules.DaysCount(start, end);

        var overlaps = _db.VacationRecords
            .Where(r => r.EmployeeId == employeeId)
            .Select(r => new { r.StartDate, r.EndDate })
            .AsEnumerable()
            .Select(r => (r.StartDate, r.EndDate))
            .ToList();

        if (RequestRules.OverlapsAny(overlaps, start, end))
            throw new InvalidOperationException(
                "Период пересекается с уже утверждённым отпуском этого сотрудника.");

        int number = _db.VacationRequests.Any()
            ? _db.VacationRequests.Max(r => r.Number) + 1
            : 1;

        var request = new VacationRequest
        {
            Number = number,
            EmployeeId = employeeId,
            VacationTypeId = vacationTypeId,
            StartDate = start.Date,
            EndDate = end.Date,
            Status = RequestStatus.New,
            CreatedAt = DateTime.Today
        };

        _db.VacationRequests.Add(request);
        _db.SaveChanges();
        return request;
    }

    /// <summary>Остаток дней вида отпуска у сотрудника за год; null — вид без лимита.</summary>
    public int? RemainingFor(int employeeId, int vacationTypeId, int year)
    {
        var type = _db.VacationTypes.Find(vacationTypeId);
        if (type is null || type.YearLimitDays <= 0)
            return null;

        var records = _db.VacationRecords
            .Where(r => r.EmployeeId == employeeId)
            .ToList();

        return RequestRules.RemainingDays(type.YearLimitDays, records, year);
    }

    /// <summary>Согласование: переход «Новое → Согласовано» с проверкой остатка дней.</summary>
    public void Agree(int requestId)
    {
        var request = Load(requestId);
        RequestRules.EnsureCanTransition(request.Status, RequestStatus.Agreed);

        var type = _db.VacationTypes.Find(request.VacationTypeId)!;
        if (type.YearLimitDays > 0)
        {
            int days = RequestRules.DaysCount(request.StartDate, request.EndDate);
            var records = _db.VacationRecords
                .Where(r => r.EmployeeId == request.EmployeeId)
                .ToList();
            int remaining = RequestRules.RemainingDays(
                type.YearLimitDays, records, request.StartDate.Year);

            if (days > remaining)
                throw new InvalidOperationException(
                    $"Недостаточно дней: остаток {remaining}, запрошено {days}.");
        }

        request.Status = RequestStatus.Agreed;
        _db.SaveChanges();
    }

    /// <summary>Записи об отпусках (приказы) за год, с фильтром по подразделению.</summary>
    public List<VacationRecord> RecordsForYear(int year, int? departmentId = null)
    {
        var query = _db.VacationRecords
            .Include(r => r.Employee)
            .ThenInclude(e => e!.Department)
            .Where(r => r.StartDate.Year == year);

        if (departmentId is int depId)
            query = query.Where(r => r.Employee!.DepartmentId == depId);

        return query.OrderBy(r => r.Employee!.FullName).ThenBy(r => r.StartDate).ToList();
    }

    /// <summary>
    /// Утверждение согласованного заявления: создаёт запись об отпуске (приказ)
    /// и резервирует дни. Обе записи сохраняются одним SaveChanges — атомарно.
    /// </summary>
    public void Approve(int requestId)
    {
        var request = Load(requestId);
        RequestRules.EnsureCanTransition(request.Status, RequestStatus.Approved);

        var type = _db.VacationTypes.Find(request.VacationTypeId)!;
        int days = RequestRules.DaysCount(request.StartDate, request.EndDate);

        // Повторная проверка остатка: между согласованием и утверждением
        // остаток мог измениться (другой приказ).
        if (type.YearLimitDays > 0)
        {
            var records = _db.VacationRecords
                .Where(r => r.EmployeeId == request.EmployeeId)
                .ToList();
            int remaining = RequestRules.RemainingDays(
                type.YearLimitDays, records, request.StartDate.Year);

            if (days > remaining)
                throw new InvalidOperationException(
                    $"Недостаточно дней: остаток {remaining}, запрошено {days}.");
        }

        // Повторная проверка пересечения с утверждёнными отпусками.
        var overlaps = _db.VacationRecords
            .Where(r => r.EmployeeId == request.EmployeeId && r.RequestId != requestId)
            .Select(r => new { r.StartDate, r.EndDate })
            .AsEnumerable()
            .Select(r => (r.StartDate, r.EndDate))
            .ToList();

        if (RequestRules.OverlapsAny(overlaps, request.StartDate, request.EndDate))
            throw new InvalidOperationException(
                "Период пересекается с уже утверждённым отпуском этого сотрудника.");

        var record = new VacationRecord
        {
            RequestId = request.Id,
            EmployeeId = request.EmployeeId,
            VacationTypeId = request.VacationTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Days = days
        };

        // одна транзакция SaveChanges: запись об отпуске + статус заявления
        _db.VacationRecords.Add(record);
        request.Status = RequestStatus.Approved;
        _db.SaveChanges();
    }

    /// <summary>Отклонение с обязательной причиной; терминальный статус.</summary>
    public void Refuse(int requestId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Укажите причину отказа.");

        var request = Load(requestId);
        RequestRules.EnsureCanTransition(request.Status, RequestStatus.Refused);

        request.Status = RequestStatus.Refused;
        request.RefuseReason = reason.Trim();
        _db.SaveChanges();
    }

    private VacationRequest Load(int id)
    {
        var request = _db.VacationRequests
            .Include(r => r.Employee)
            .Include(r => r.VacationType)
            .FirstOrDefault(r => r.Id == id)
            ?? throw new InvalidOperationException("Заявление не найдено.");

        return request;
    }
}

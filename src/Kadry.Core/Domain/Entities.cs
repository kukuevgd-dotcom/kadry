namespace Kadry.Core.Domain;

/// <summary>Подразделение организации.</summary>
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Employee> Employees { get; set; } = new();
}

/// <summary>Должность.</summary>
public class Position
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Employee> Employees { get; set; } = new();
}

/// <summary>Вид отпуска из справочника.</summary>
public class VacationType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Годовой лимит дней; 0 — без лимита (учебный, за свой счёт).</summary>
    public int YearLimitDays { get; set; }

    /// <summary>Отпуск без сохранения заработной платы: из лимита не вычитается.</summary>
    public bool IsUnpaid { get; set; }

    public List<VacationRequest> Requests { get; set; } = new();
    public List<VacationRecord> Records { get; set; } = new();
}

/// <summary>Сотрудник организации.</summary>
public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PersonnelNumber { get; set; } = string.Empty;

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int PositionId { get; set; }
    public Position? Position { get; set; }

    public DateTime HiredAt { get; set; }

    /// <summary>Дата увольнения; null — работает.</summary>
    public DateTime? FiredAt { get; set; }

    public List<VacationRequest> Requests { get; set; } = new();
    public List<VacationRecord> Records { get; set; } = new();

    public bool IsActive => FiredAt is null;
}

/// <summary>Заявление на отпуск.</summary>
public class VacationRequest
{
    public int Id { get; set; }

    /// <summary>Сквозной номер заявления.</summary>
    public int Number { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int VacationTypeId { get; set; }
    public VacationType? VacationType { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.New;

    public DateTime CreatedAt { get; set; } = DateTime.Today;

    /// <summary>Причина отклонения; заполняется при отказе.</summary>
    public string? RefuseReason { get; set; }

    /// <summary>Приказ (запись об отпуске); создаётся при утверждении.</summary>
    public VacationRecord? Record { get; set; }
}

/// <summary>Запись об отпуске — проведённый приказом отпуск.</summary>
public class VacationRecord
{
    public int Id { get; set; }

    /// <summary>Заявление, по которому издан приказ (связь один к одному).</summary>
    public int RequestId { get; set; }
    public VacationRequest? Request { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int VacationTypeId { get; set; }
    public VacationType? VacationType { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>Число календарных дней отпуска.</summary>
    public int Days { get; set; }
}

/// <summary>Учётная запись пользователя приложения.</summary>
public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
    public UserRole Role { get; set; } = UserRole.HrOfficer;

    /// <summary>Сотрудник, за которым закреплена учётная запись; у руководителя задаёт подразделение.</summary>
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}

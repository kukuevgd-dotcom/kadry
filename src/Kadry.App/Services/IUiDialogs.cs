using Kadry.Core.Domain;

namespace Kadry.App.Services;

/// <summary>Данные сотрудника, введённые в диалоге.</summary>
public record EmployeeInput(
    string FullName,
    string PersonnelNumber,
    int DepartmentId,
    int PositionId,
    DateTime HiredAt);

/// <summary>Данные вида отпуска из диалога.</summary>
public record VacationTypeInput(
    string Name,
    int YearLimitDays,
    bool IsUnpaid);

/// <summary>Данные заявления на отпуск из диалога.</summary>
public record RequestInput(
    int EmployeeId,
    int VacationTypeId,
    DateTime StartDate,
    DateTime EndDate);

/// <summary>Диалоги интерфейса. Реализация открывает окна поверх главного.</summary>
public interface IUiDialogs
{
    /// <summary>Диалог регистрации заявления на отпуск. null — отмена.</summary>
    RequestInput? EditRequest(
        IReadOnlyList<Employee> employees,
        IReadOnlyList<VacationType> vacationTypes);

    /// <summary>Диалог сотрудника. null — отмена. initial не задан — приём нового.</summary>
    EmployeeInput? EditEmployee(
        IReadOnlyList<Department> departments,
        IReadOnlyList<Position> positions,
        EmployeeInput? initial = null);

    /// <summary>Диалог одного текстового поля. null — отмена.</summary>
    string? EditText(string title, string label, string initial);

    /// <summary>Диалог вида отпуска. null — отмена.</summary>
    VacationTypeInput? EditVacationType(VacationTypeInput? initial = null);

    /// <summary>Выбор даты. null — отмена.</summary>
    DateTime? PickDate(string title, DateTime initial);

    /// <summary>Подтверждение (Да/Нет).</summary>
    bool Confirm(string title, string message);

    /// <summary>Сообщение об ошибке с текстом причины.</summary>
    void Error(string message);
}

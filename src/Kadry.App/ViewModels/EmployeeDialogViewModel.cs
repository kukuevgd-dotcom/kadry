using Kadry.App.Infrastructure;
using Kadry.App.Services;
using Kadry.Core.Domain;

namespace Kadry.App.ViewModels;

/// <summary>Модель представления диалога приёма/редактирования сотрудника.</summary>
public class EmployeeDialogViewModel : ViewModelBase
{
    private readonly IReadOnlyList<Department> _departments;
    private readonly IReadOnlyList<Position> _positions;

    private string _fullName = string.Empty;
    private string _personnelNumber = string.Empty;
    private Department? _department;
    private Position? _position;
    private DateTime? _hiredAt;
    private string? _error;

    public EmployeeDialogViewModel(IReadOnlyList<Department> departments,
                                   IReadOnlyList<Position> positions,
                                   EmployeeInput? initial = null)
    {
        _departments = departments;
        _positions = positions;
        Title = initial is null ? "Приём сотрудника" : "Изменение сотрудника";

        if (initial is not null)
        {
            _fullName = initial.FullName;
            _personnelNumber = initial.PersonnelNumber;
            _department = departments.FirstOrDefault(d => d.Id == initial.DepartmentId);
            _position = positions.FirstOrDefault(p => p.Id == initial.PositionId);
            _hiredAt = initial.HiredAt;
        }

        OkCommand = new RelayCommand(_ => Submit(), _ => true);
    }

    public string Title { get; }

    public event EventHandler? Submitted;

    public RelayCommand OkCommand { get; }

    /// <summary>Результат диалога; заполняется при успешной валидации.</summary>
    public EmployeeInput? Result { get; private set; }

    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
    }

    public string PersonnelNumber
    {
        get => _personnelNumber;
        set => SetProperty(ref _personnelNumber, value);
    }

    public IReadOnlyList<Department> Departments => _departments;

    public IReadOnlyList<Position> Positions => _positions;

    public Department? Department
    {
        get => _department;
        set => SetProperty(ref _department, value);
    }

    public Position? Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }

    public DateTime? HiredAt
    {
        get => _hiredAt;
        set => SetProperty(ref _hiredAt, value);
    }

    public string? Error
    {
        get => _error;
        private set => SetProperty(ref _error, value);
    }

    private void Submit()
    {
        Error = null;

        if (string.IsNullOrWhiteSpace(FullName))
        {
            Error = "ФИО не заполнено.";
            return;
        }

        if (string.IsNullOrWhiteSpace(PersonnelNumber))
        {
            Error = "Табельный номер не заполнен.";
            return;
        }

        if (Department is null)
        {
            Error = "Не выбран отдел.";
            return;
        }

        if (Position is null)
        {
            Error = "Не выбрана должность.";
            return;
        }

        if (HiredAt is not DateTime hired)
        {
            Error = "Не указана дата приёма.";
            return;
        }

        if (hired.Date > DateTime.Today)
        {
            Error = "Дата приёма не может быть в будущем.";
            return;
        }

        Result = new EmployeeInput(
            FullName.Trim(),
            PersonnelNumber.Trim(),
            Department.Id,
            Position.Id,
            hired.Date);

        Submitted?.Invoke(this, EventArgs.Empty);
    }
}

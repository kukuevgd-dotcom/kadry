using Kadry.App.Infrastructure;
using Kadry.App.Services;
using Kadry.Core.Domain;
using Kadry.Core.Services;

namespace Kadry.App.ViewModels;

/// <summary>Модель представления диалога регистрации заявления на отпуск.</summary>
public class RequestDialogViewModel : ViewModelBase
{
    private readonly VacationRequestService _requestService;
    private readonly IReadOnlyList<Employee> _employees;
    private readonly IReadOnlyList<VacationType> _vacationTypes;

    private Employee? _employee;
    private VacationType? _vacationType;
    private DateTime? _startDate;
    private DateTime? _endDate;
    private string? _error;

    public RequestDialogViewModel(VacationRequestService requestService,
                                  IReadOnlyList<Employee> employees,
                                  IReadOnlyList<VacationType> vacationTypes)
    {
        _requestService = requestService;
        _employees = employees;
        _vacationTypes = vacationTypes;

        OkCommand = new RelayCommand(_ => Submit(), _ => true);
    }

    public event EventHandler? Submitted;

    public RelayCommand OkCommand { get; }

    /// <summary>Результат диалога; заполняется при успешной валидации.</summary>
    public RequestInput? Result { get; private set; }

    public IReadOnlyList<Employee> Employees => _employees;

    public IReadOnlyList<VacationType> VacationTypes => _vacationTypes;

    public Employee? Employee
    {
        get => _employee;
        set
        {
            if (SetProperty(ref _employee, value))
                OnPropertyChanged(nameof(RemainingText));
        }
    }

    public VacationType? VacationType
    {
        get => _vacationType;
        set
        {
            if (SetProperty(ref _vacationType, value))
                OnPropertyChanged(nameof(RemainingText));
        }
    }

    public DateTime? StartDate
    {
        get => _startDate;
        set
        {
            if (SetProperty(ref _startDate, value))
            {
                OnPropertyChanged(nameof(DaysText));
                OnPropertyChanged(nameof(RemainingText));
            }
        }
    }

    public DateTime? EndDate
    {
        get => _endDate;
        set
        {
            if (SetProperty(ref _endDate, value))
            {
                OnPropertyChanged(nameof(DaysText));
                OnPropertyChanged(nameof(RemainingText));
            }
        }
    }

    public string DaysText
    {
        get
        {
            if (StartDate is not DateTime start || EndDate is not DateTime end)
                return "—";

            try
            {
                return $"{RequestRules.DaysCount(start, end)} дн.";
            }
            catch (ArgumentException)
            {
                return "даты заданы неверно";
            }
        }
    }

    public string RemainingText
    {
        get
        {
            if (Employee is null || VacationType is null)
                return "—";

            int year = StartDate is DateTime start ? start.Year : DateTime.Today.Year;
            int? remaining = _requestService.RemainingFor(Employee.Id, VacationType.Id, year);

            return remaining is null ? "без лимита" : $"{remaining} дн.";
        }
    }

    public string? Error
    {
        get => _error;
        private set => SetProperty(ref _error, value);
    }

    private void Submit()
    {
        Error = null;

        if (Employee is null)
        {
            Error = "Не выбран сотрудник.";
            return;
        }

        if (VacationType is null)
        {
            Error = "Не выбран вид отпуска.";
            return;
        }

        if (StartDate is not DateTime start || EndDate is not DateTime end)
        {
            Error = "Укажите период отпуска.";
            return;
        }

        if (end < start)
        {
            Error = "Дата окончания раньше даты начала.";
            return;
        }

        Result = new RequestInput(Employee.Id, VacationType.Id, start.Date, end.Date);
        Submitted?.Invoke(this, EventArgs.Empty);
    }
}

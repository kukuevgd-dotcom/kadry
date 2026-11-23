using System.Collections.ObjectModel;
using Kadry.App.Infrastructure;
using Kadry.App.Services;
using Kadry.Core.Domain;
using Kadry.Core.Services;

namespace Kadry.App.ViewModels;

/// <summary>Строка журнала заявлений.</summary>
public class RequestRow
{
    public int Id { get; init; }
    public string Number { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public string Employee { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
    public string Period { get; init; } = string.Empty;
    public int Days { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public string RefuseReason { get; init; } = string.Empty;
}

/// <summary>Экран «Заявления на отпуск»: журнал, фильтры, статусы.</summary>
public class RequestsViewModel : ViewModelBase
{
    private readonly VacationRequestService _service;
    private readonly EmployeeService _employeeService;
    private readonly DirectoryService _directoryService;
    private readonly IUiDialogs _dialogs;

    private string _searchText = string.Empty;
    private string _statusFilter = "все";
    private RequestRow? _selectedRequest;

    public RequestsViewModel(VacationRequestService service,
                             EmployeeService employeeService,
                             DirectoryService directoryService,
                             IUiDialogs dialogs)
    {
        _service = service;
        _employeeService = employeeService;
        _directoryService = directoryService;
        _dialogs = dialogs;

        RegisterCommand = new RelayCommand(_ => Register());
        AgreeCommand = new RelayCommand(_ => Agree(), _ => SelectedRequest is { StatusName: "Новое" });
        ApproveCommand = new RelayCommand(_ => Approve(), _ => SelectedRequest is { StatusName: "Согласовано" });
        RefuseCommand = new RelayCommand(_ => Refuse(),
            _ => SelectedRequest is { StatusName: "Новое" or "Согласовано" });

        Reload();
    }

    public ObservableCollection<RequestRow> Requests { get; } = new();

    public IReadOnlyList<string> StatusFilters { get; } =
        new[] { "все", "новое", "согласовано", "утверждено", "отклонено" };

    public RelayCommand RegisterCommand { get; }

    public RelayCommand AgreeCommand { get; }

    public RelayCommand ApproveCommand { get; }

    public RelayCommand RefuseCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                Reload();
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
                Reload();
        }
    }

    public RequestRow? SelectedRequest
    {
        get => _selectedRequest;
        set => SetProperty(ref _selectedRequest, value);
    }

    private void Reload()
    {
        RequestStatus? status = _statusFilter switch
        {
            "новое" => RequestStatus.New,
            "согласовано" => RequestStatus.Agreed,
            "утверждено" => RequestStatus.Approved,
            "отклонено" => RequestStatus.Refused,
            _ => null
        };

        Requests.Clear();
        foreach (var r in _service.List(status, null, null, _searchText))
        {
            Requests.Add(new RequestRow
            {
                Id = r.Id,
                Number = $"№ {r.Number}",
                CreatedAt = r.CreatedAt.ToString("dd.MM.yyyy"),
                Employee = r.Employee?.FullName ?? string.Empty,
                Department = r.Employee?.Department?.Name ?? string.Empty,
                TypeName = r.VacationType?.Name ?? string.Empty,
                Period = $"{r.StartDate:dd.MM.yyyy} — {r.EndDate:dd.MM.yyyy}",
                Days = RequestRules.DaysCount(r.StartDate, r.EndDate),
                StatusName = RequestRules.NameOf(r.Status),
                RefuseReason = r.RefuseReason ?? string.Empty
            });
        }
    }

    private void Register()
    {
        var employees = _employeeService.List(onlyActive: true);
        var input = _dialogs.EditRequest(employees, _directoryService.VacationTypes());
        if (input is null)
            return;

        try
        {
            _service.Register(input.EmployeeId, input.VacationTypeId, input.StartDate, input.EndDate);
            Reload();
        }
        catch (Exception ex)
        {
            _dialogs.Error(ex.Message);
        }
    }

    private void Agree()
    {
        if (SelectedRequest is null)
            return;

        if (!_dialogs.Confirm("Согласование",
                $"Согласовать заявление {SelectedRequest.Number} ({SelectedRequest.Employee})?"))
            return;

        try
        {
            _service.Agree(SelectedRequest.Id);
            Reload();
        }
        catch (Exception ex)
        {
            _dialogs.Error(ex.Message);
        }
    }

    private void Approve()
    {
        if (SelectedRequest is null)
            return;

        if (!_dialogs.Confirm("Утверждение — издание приказа",
                $"Утвердить заявление {SelectedRequest.Number} ({SelectedRequest.Employee}, " +
                $"{SelectedRequest.Period}, {SelectedRequest.Days} дн.)?\n" +
                "Будет издан приказ, дни зарезвируются."))
            return;

        try
        {
            _service.Approve(SelectedRequest.Id);
            Reload();
        }
        catch (Exception ex)
        {
            _dialogs.Error(ex.Message);
        }
    }

    private void Refuse()
    {
        if (SelectedRequest is null)
            return;

        string? reason = _dialogs.EditText("Отклонение заявления",
            "Причина отказа", string.Empty);
        if (string.IsNullOrWhiteSpace(reason))
            return;

        try
        {
            _service.Refuse(SelectedRequest.Id, reason);
            Reload();
        }
        catch (Exception ex)
        {
            _dialogs.Error(ex.Message);
        }
    }
}

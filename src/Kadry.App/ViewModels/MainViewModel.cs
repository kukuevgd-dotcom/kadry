using Kadry.App.Infrastructure;
using Kadry.App.Services;
using Kadry.Core.Data;
using Kadry.Core.Services;

namespace Kadry.App.ViewModels;

/// <summary>Разделы приложения (боковое меню).</summary>
public enum Section
{
    Dashboard,
    Employees,
    Requests,
    Schedule,
    Directories,
    Reports
}

/// <summary>Модель представления главного окна: навигация по разделам.</summary>
public class MainViewModel : ViewModelBase
{
    private readonly EmployeesViewModel _employees;
    private readonly DirectoriesViewModel _directories;
    private readonly RequestsViewModel _requests;
    private readonly ScheduleViewModel _schedule;

    private Section _currentSection = Section.Dashboard;

    public MainViewModel(KadryContext db, IUiDialogs dialogs)
    {
        var employeeService = new EmployeeService(db);
        var directoryService = new DirectoryService(db);
        var requestService = new VacationRequestService(db);

        _employees = new EmployeesViewModel(employeeService, directoryService, dialogs);
        _directories = new DirectoriesViewModel(directoryService, dialogs);
        _requests = new RequestsViewModel(requestService, employeeService, directoryService, dialogs);
        _schedule = new ScheduleViewModel(requestService, directoryService);

        NavigateCommand = new RelayCommand(parameter =>
        {
            if (parameter is string name && Enum.TryParse<Section>(name, out var section))
                CurrentSection = section;
        });
    }

    public RelayCommand NavigateCommand { get; }

    public Section CurrentSection
    {
        get => _currentSection;
        set
        {
            if (SetProperty(ref _currentSection, value))
            {
                OnPropertyChanged(nameof(SectionTitle));
                OnPropertyChanged(nameof(CurrentPage));
            }
        }
    }

    /// <summary>Модель представления текущего раздела; null — раздел ещё не реализован.</summary>
    public object? CurrentPage => CurrentSection switch
    {
        Section.Employees => _employees,
        Section.Directories => _directories,
        Section.Requests => _requests,
        Section.Schedule => _schedule,
        _ => null
    };

    public string SectionTitle => CurrentSection switch
    {
        Section.Dashboard => "Панель мониторинга",
        Section.Employees => "Сотрудники",
        Section.Requests => "Заявления на отпуск",
        Section.Schedule => "График отпусков",
        Section.Directories => "Справочники",
        Section.Reports => "Отчёты",
        _ => "Кадры"
    };

    public string UserName { get; set; } = "Кадровик";

    public string UserRole { get; set; } = "кадровик";
}

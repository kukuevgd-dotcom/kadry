using Kadry.App.Infrastructure;

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
    private Section _currentSection = Section.Dashboard;
    private string _userName = "Кадровик";
    private string _userRole = "кадровик";

    public MainViewModel()
    {
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
                OnPropertyChanged(nameof(SectionTitle));
        }
    }

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

    /// <summary>Имя вошедшего пользователя (заполняется после авторизации).</summary>
    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    /// <summary>Роль вошедшего пользователя.</summary>
    public string UserRole
    {
        get => _userRole;
        set => SetProperty(ref _userRole, value);
    }
}

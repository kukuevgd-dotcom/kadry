using System.Windows;
using Kadry.App.Services;
using Kadry.App.ViewModels;
using Kadry.App.Views;
using Kadry.Core.Domain;
using Kadry.Core.Services;

namespace Kadry.App.Services;

/// <summary>Открытие модальных диалогов поверх главного окна.</summary>
public class WpfDialogs : IUiDialogs
{
    private readonly Func<Window> _ownerAccessor;
    private readonly VacationRequestService _requestService;

    public WpfDialogs(Func<Window> ownerAccessor, VacationRequestService requestService)
    {
        _ownerAccessor = ownerAccessor;
        _requestService = requestService;
    }

    private Window Owner => _ownerAccessor();

    public RequestInput? EditRequest(
        IReadOnlyList<Employee> employees,
        IReadOnlyList<VacationType> vacationTypes)
    {
        var vm = new RequestDialogViewModel(_requestService, employees, vacationTypes);
        var window = new RequestDialog
        {
            Owner = Owner,
            DataContext = vm
        };
        vm.Submitted += (_, _) => window.DialogResult = true;

        return window.ShowDialog() == true ? vm.Result : null;
    }

    public EmployeeInput? EditEmployee(
        IReadOnlyList<Department> departments,
        IReadOnlyList<Position> positions,
        EmployeeInput? initial = null)
    {
        var vm = new EmployeeDialogViewModel(departments, positions, initial);
        var window = new EmployeeDialog
        {
            Owner = Owner,
            DataContext = vm
        };
        vm.Submitted += (_, _) => window.DialogResult = true;

        return window.ShowDialog() == true ? vm.Result : null;
    }

    public string? EditText(string title, string label, string initial)
        => TextDialog.Show(Owner, title, label, initial);

    public VacationTypeInput? EditVacationType(VacationTypeInput? initial = null)
        => VacationTypeDialog.Show(Owner, initial);

    public DateTime? PickDate(string title, DateTime initial)
        => DateDialog.Show(Owner, title, initial);

    public string? SaveFile(string defaultName)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = defaultName,
            DefaultExt = ".csv",
            Filter = "CSV-файл (*.csv)|*.csv",
            AddExtension = true
        };

        return dialog.ShowDialog(Owner) == true ? dialog.FileName : null;
    }

    public bool Confirm(string title, string message)
        => MessageBox.Show(Owner, message, title,
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void Error(string message)
        => MessageBox.Show(Owner, message, "Кадры",
            MessageBoxButton.OK, MessageBoxImage.Error);
}

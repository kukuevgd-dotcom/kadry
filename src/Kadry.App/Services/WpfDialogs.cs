using System.Windows;
using Kadry.App.Services;
using Kadry.App.ViewModels;
using Kadry.App.Views;
using Kadry.Core.Domain;

namespace Kadry.App.Services;

/// <summary>Открытие модальных диалогов поверх главного окна.</summary>
public class WpfDialogs : IUiDialogs
{
    private readonly Func<Window> _ownerAccessor;

    public WpfDialogs(Func<Window> ownerAccessor)
    {
        _ownerAccessor = ownerAccessor;
    }

    private Window Owner => _ownerAccessor();

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

    public bool Confirm(string title, string message)
        => MessageBox.Show(Owner, message, title,
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void Error(string message)
        => MessageBox.Show(Owner, message, "Кадры",
            MessageBoxButton.OK, MessageBoxImage.Error);
}

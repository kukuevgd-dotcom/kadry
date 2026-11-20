using System.Windows;
using Kadry.App.Services;

namespace Kadry.App.Views;

/// <summary>Диалог вида отпуска: название, лимит дней, признак «без оплаты».</summary>
public partial class VacationTypeDialog : Window
{
    private VacationTypeDialog(VacationTypeInput? initial)
    {
        InitializeComponent();

        if (initial is not null)
        {
            Title = "Изменение вида отпуска";
            NameBox.Text = initial.Name;
            LimitBox.Text = initial.YearLimitDays.ToString();
            UnpaidBox.IsChecked = initial.IsUnpaid;
        }
        else
        {
            Title = "Новый вид отпуска";
            LimitBox.Text = "0";
        }
    }

    public static VacationTypeInput? Show(Window owner, VacationTypeInput? initial)
    {
        var dialog = new VacationTypeDialog(initial) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.BuildResult() : null;
    }

    private VacationTypeInput BuildResult() => new(
        NameBox.Text.Trim(),
        int.TryParse(LimitBox.Text.Trim(), out int limit) ? limit : -1,
        UnpaidBox.IsChecked == true);

    private void OnOk(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            ErrorText.Text = "Название не заполнено.";
            return;
        }

        if (!int.TryParse(LimitBox.Text.Trim(), out int limit) || limit < 0)
        {
            ErrorText.Text = "Лимит дней — целое неотрицательное число.";
            return;
        }

        DialogResult = true;
    }
}

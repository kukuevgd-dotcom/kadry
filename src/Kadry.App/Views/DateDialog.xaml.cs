using System.Windows;

namespace Kadry.App.Views;

/// <summary>Диалог выбора даты.</summary>
public partial class DateDialog : Window
{
    private DateDialog(string title, DateTime initial)
    {
        InitializeComponent();
        Title = title;
        LabelText.Text = title;
        Picker.SelectedDate = initial;
    }

    public static DateTime? Show(Window owner, string title, DateTime initial)
    {
        var dialog = new DateDialog(title, initial) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.Picker.SelectedDate : null;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (Picker.SelectedDate is null)
            return;

        DialogResult = true;
    }
}

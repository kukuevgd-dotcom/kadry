using System.Windows;

namespace Kadry.App.Views;

/// <summary>Диалог одного текстового поля (название отдела/должности).</summary>
public partial class TextDialog : Window
{
    private TextDialog(string title, string label, string initial)
    {
        InitializeComponent();
        Title = title;
        LabelText.Text = label;
        ValueBox.Text = initial;
        ValueBox.Focus();
        ValueBox.SelectAll();
    }

    public static string? Show(Window owner, string title, string label, string initial)
    {
        var dialog = new TextDialog(title, label, initial) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.ValueBox.Text.Trim() : null;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ValueBox.Text))
        {
            ErrorText.Text = "Заполните поле.";
            return;
        }

        DialogResult = true;
    }
}

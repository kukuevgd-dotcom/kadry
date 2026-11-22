using System.IO;
using System.Windows;
using Kadry.App.Services;
using Kadry.App.ViewModels;
using Kadry.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Kadry.App;

/// <summary>Точка входа: подготовка базы данных и запуск главного окна.</summary>
public partial class App : Application
{
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(KadryContext.DefaultDatabasePath)!);

            var options = new DbContextOptionsBuilder<KadryContext>()
                .UseSqlite($"Data Source={KadryContext.DefaultDatabasePath}")
                .Options;

            var db = new KadryContext(options);
            DbInitializer.Initialize(db);

            _mainWindow = new MainWindow();
            var requestService = new Kadry.Core.Services.VacationRequestService(db);
            var dialogs = new WpfDialogs(() => _mainWindow!, requestService);
            _mainWindow.DataContext = new MainViewModel(db, dialogs);
            _mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не удалось запустить приложение:\n{ex.Message}",
                "Кадры", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}

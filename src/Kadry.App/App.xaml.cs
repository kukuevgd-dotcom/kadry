using System.IO;
using System.Windows;
using Kadry.App.ViewModels;
using Kadry.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Kadry.App;

/// <summary>Точка входа: подготовка базы данных и запуск главного окна.</summary>
public partial class App : Application
{
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

            var window = new MainWindow
            {
                DataContext = new MainViewModel()
            };
            window.Show();
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

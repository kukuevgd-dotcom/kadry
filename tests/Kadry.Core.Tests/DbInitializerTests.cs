using Kadry.Core.Data;
using Kadry.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kadry.Core.Tests;

public class DbInitializerTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(), $"kadry_test_{Guid.NewGuid():N}.db");

    private KadryContext CreateContext() => new(
        new DbContextOptionsBuilder<KadryContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options);

    [Fact]
    public void Initialize_CreatesSchemaAndSeedData()
    {
        using (var db = CreateContext())
        {
            DbInitializer.Initialize(db);

            Assert.Equal(4, db.Departments.Count());
            Assert.Equal(5, db.Positions.Count());

            var mainType = db.VacationTypes.Single(t => t.YearLimitDays == 28);
            Assert.Equal("Основной ежегодный оплачиваемый", mainType.Name);
            Assert.True(db.VacationTypes.Single(t => t.Name == "Без сохранения заработной платы").IsUnpaid);

            var admin = db.Users.Single();
            Assert.Equal("admin", admin.Login);
            Assert.True(PasswordHasher.Verify("admin", admin.PasswordSalt, admin.PasswordHash));
        }

        Assert.True(File.Exists(_dbPath));
    }

    [Fact]
    public void Initialize_Twice_DoesNotDuplicateSeed()
    {
        using (var db = CreateContext())
        {
            DbInitializer.Initialize(db);
        }

        using (var db = CreateContext())
        {
            DbInitializer.Initialize(db);

            Assert.Equal(4, db.Departments.Count());
            Assert.Equal(1, db.Users.Count());
        }
    }

    public void Dispose()
    {
        // соединения SQLite кешируются в пуле — перед удалением файла пул очищается
        SqliteConnection.ClearAllPools();

        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var file = _dbPath + suffix;
            if (File.Exists(file))
                File.Delete(file);
        }
    }
}

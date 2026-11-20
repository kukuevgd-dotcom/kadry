using Kadry.Core.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kadry.Core.Tests;

/// <summary>База для тестов сервисов: свой файл SQLite на каждый тест.</summary>
public abstract class IsolatedDbTests : IDisposable
{
    private readonly List<string> _paths = new();

    /// <summary>Создаёт свежую базу со схемой и стартовыми справочниками.</summary>
    protected KadryContext NewDb()
    {
        string path = Path.Combine(Path.GetTempPath(), $"kadry_{Guid.NewGuid():N}.db");
        _paths.Add(path);

        var db = new KadryContext(
            new DbContextOptionsBuilder<KadryContext>()
                .UseSqlite($"Data Source={path}")
                .Options);
        DbInitializer.Initialize(db);
        return db;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        foreach (string path in _paths)
            foreach (string suffix in new[] { "", "-wal", "-shm" })
                if (File.Exists(path + suffix))
                    File.Delete(path + suffix);
    }
}

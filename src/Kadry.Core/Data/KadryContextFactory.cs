using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kadry.Core.Data;

/// <summary>
/// Фабрика контекста для инструментов EF Core (dotnet ef migrations).
/// Приложение создаёт контекст само; фабрика нужна только на этапе разработки.
/// </summary>
public class KadryContextFactory : IDesignTimeDbContextFactory<KadryContext>
{
    public KadryContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<KadryContext>()
            .UseSqlite($"Data Source={KadryContext.DefaultDatabasePath}")
            .Options;
        return new KadryContext(options);
    }
}

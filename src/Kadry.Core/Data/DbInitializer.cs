using Kadry.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kadry.Core.Data;

/// <summary>Подготовка базы: создание схемы миграциями, режим WAL, стартовые данные.</summary>
public static class DbInitializer
{
    public static void Initialize(KadryContext db)
    {
        db.Database.Migrate();
        db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        Seed(db);
    }

    private static void Seed(KadryContext db)
    {
        if (!db.Departments.Any())
        {
            db.Departments.AddRange(
                new Department { Name = "Администрация" },
                new Department { Name = "Бухгалтерия" },
                new Department { Name = "Отдел разработки" },
                new Department { Name = "Отдел продаж" });
        }

        if (!db.Positions.Any())
        {
            db.Positions.AddRange(
                new Position { Name = "Директор" },
                new Position { Name = "Бухгалтер" },
                new Position { Name = "Инженер-программист" },
                new Position { Name = "Менеджер по продажам" },
                new Position { Name = "Кадровик" });
        }

        if (!db.VacationTypes.Any())
        {
            db.VacationTypes.AddRange(
                new VacationType { Name = "Основной ежегодный оплачиваемый", YearLimitDays = 28 },
                new VacationType { Name = "Дополнительный", YearLimitDays = 7 },
                new VacationType { Name = "Без сохранения заработной платы", IsUnpaid = true },
                new VacationType { Name = "Учебный" });
        }

        db.SaveChanges();

        if (!db.Users.Any())
        {
            var salt = Services.PasswordHasher.GenerateSalt();
            db.Users.Add(new User
            {
                Login = "admin",
                PasswordSalt = salt,
                PasswordHash = Services.PasswordHasher.Hash("admin", salt),
                Role = UserRole.HrOfficer
            });
            db.SaveChanges();
        }
    }
}

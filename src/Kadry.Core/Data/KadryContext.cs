using Kadry.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kadry.Core.Data;

/// <summary>Контекст EF Core: SQLite, файл в каталоге данных пользователя.</summary>
public class KadryContext : DbContext
{
    public KadryContext(DbContextOptions<KadryContext> options) : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<VacationType> VacationTypes => Set<VacationType>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<VacationRequest> VacationRequests => Set<VacationRequest>();
    public DbSet<VacationRecord> VacationRecords => Set<VacationRecord>();
    public DbSet<User> Users => Set<User>();

    /// <summary>Путь к файлу базы по умолчанию.</summary>
    public static string DefaultDatabasePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Kadry", "kadry.db");

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Department>(e =>
        {
            e.ToTable("Departments");
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
        });

        mb.Entity<Position>(e =>
        {
            e.ToTable("Positions");
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
        });

        mb.Entity<VacationType>(e =>
        {
            e.ToTable("VacationTypes");
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
        });

        mb.Entity<Employee>(e =>
        {
            e.ToTable("Employees");
            e.Property(x => x.FullName).IsRequired().HasMaxLength(150);
            e.Property(x => x.PersonnelNumber).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.PersonnelNumber).IsUnique();
            e.HasIndex(x => x.DepartmentId);
            e.HasOne(x => x.Department).WithMany(d => d.Employees)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Position).WithMany(p => p.Employees)
                .HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<VacationRequest>(e =>
        {
            e.ToTable("VacationRequests");
            e.HasIndex(x => x.Number).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.EmployeeId);
            e.Property(x => x.RefuseReason).HasMaxLength(500);
            e.HasOne(x => x.Employee).WithMany(p => p.Requests)
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.VacationType).WithMany(t => t.Requests)
                .HasForeignKey(x => x.VacationTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<VacationRecord>(e =>
        {
            e.ToTable("VacationRecords");
            e.HasIndex(x => x.RequestId).IsUnique();
            e.HasIndex(x => x.EmployeeId);
            e.HasOne(x => x.Request).WithOne(r => r.Record)
                .HasForeignKey<VacationRecord>(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Employee).WithMany(p => p.Records)
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.VacationType).WithMany(t => t.Records)
                .HasForeignKey(x => x.VacationTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.Property(x => x.Login).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Login).IsUnique();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.PasswordSalt).IsRequired();
            e.HasIndex(x => x.EmployeeId).IsUnique();
            e.HasOne(x => x.Employee).WithMany()
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

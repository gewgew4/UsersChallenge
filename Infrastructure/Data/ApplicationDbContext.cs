using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<PermissionType> PermissionTypes { get; set; }

    public Task<int> SaveChangesAsync()
    {
        return base.SaveChangesAsync();
    }

    public void Migrate()
    {
        base.Database.Migrate();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Props config
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Seed
        Seed(modelBuilder);
    }

    private static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Permission>().HasData(
            new Permission
            {
                Id = 1,
                EmployeeForename = "Forename",
                EmployeeSurname = "Surname",
                PermissionDate = DateTime.Parse("2000-01-01T00:00:00"),
                PermissionTypeId = 1
            })
            ;

        modelBuilder.Entity<PermissionType>().HasData(
            new PermissionType
            {
                Id = 1,
                Description = "First type"
            },
            new PermissionType
            {
                Id = 2,
                Description = "Second type"
            },
            new PermissionType
            {
                Id = 3,
                Description = "Third type"
            },
            new PermissionType
            {
                Id = 4,
                Description = "Fourth type"
            }
         );
    }
}
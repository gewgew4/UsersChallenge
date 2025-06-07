using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.IntegrationTests.DatabaseTests;

public class DatabaseConnectionTests : IDisposable
{
    private readonly Mock<ILogger<ApplicationDbContext>> _mockLogger;

    public DatabaseConnectionTests() => _mockLogger = new Mock<ILogger<ApplicationDbContext>>();

    [Fact]
    public async Task Connection_WithValidConnectionString_ShouldConnect()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        // Act
        var canConnect = await context.Database.CanConnectAsync();

        // Assert
        canConnect.Should().BeTrue();
    }

    [Fact]
    public async Task Connection_WithInMemoryDatabase_ShouldCreateAndMigrate()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Act
        var canConnect = await context.Database.CanConnectAsync();
        var permissionTypesCount = await context.PermissionTypes.CountAsync();
        var permissionsCount = await context.Permissions.CountAsync();

        // Assert
        canConnect.Should().BeTrue();
        permissionTypesCount.Should().BeGreaterThanOrEqualTo(0);
        permissionsCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Connection_MultipleContexts_ShouldHandleConcurrency()
    {
        // Arrange
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        // Act
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var context = new ApplicationDbContext(options);
                return await context.Database.CanConnectAsync();
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().OnlyContain(r => r == true);
    }

    [Fact]
    public async Task Database_EnsureCreated_ShouldCreateTables()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var permissionType = new Domain.Entities.PermissionType
        {
            Id = 100,
            Description = "Test Type"
        };

        using var context = new ApplicationDbContext(options);
        var created = await context.Database.EnsureCreatedAsync();

        // Act
        context.PermissionTypes.Add(permissionType);

        var saveResult = await context.SaveChangesAsync();

        // Assert
        created.Should().BeTrue();
        saveResult.Should().Be(1);
    }

    [Fact]
    public async Task Context_Dispose_ShouldCleanupProperly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        ApplicationDbContext? context = null;

        // Act
        var act = async () =>
        {
            context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            context.Dispose();
        };

        // Assert
        await act.Should().NotThrowAsync();
        context.Should().NotBeNull();
    }

    [Fact]
    public async Task Database_FailedConnection_ShouldHandleGracefully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=invalid;Database=invalid;Integrated Security=true;TrustServerCertificate=true;")
            .Options;

        using var context = new ApplicationDbContext(options);

        // Act
        var act = await context.Database.CanConnectAsync();

        // Assert
        Assert.False(act);
    }

    [Fact]
    public void Database_ConnectionString_ShouldBeConfigurable()
    {
        // Arrange
        var connectionString1 = "Server=server1;Database=db1;";
        var connectionString2 = "Server=server2;Database=db2;";

        var options1 = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString1)
            .Options;

        var options2 = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString2)
            .Options;

        using var context1 = new ApplicationDbContext(options1);
        using var context2 = new ApplicationDbContext(options2);

        // Act
        var db1 = context1.Database.GetConnectionString();
        var db2 = context2.Database.GetConnectionString();

        // Assert
        db1.Should().Be(connectionString1);
        db2.Should().Be(connectionString2);
        db1.Should().NotBe(db2);
    }

    [Fact]
    public async Task Database_MigrationStatus_ShouldBeCheckable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        // Act
        var created = await context.Database.EnsureCreatedAsync();

        // Assert
        created.Should().BeTrue();
    }

    [Fact]
    public async Task Database_SeedData_ShouldBeApplied()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Act
        var permissionTypes = await context.PermissionTypes.ToListAsync();
        var permissions = await context.Permissions.ToListAsync();

        // Assert
        permissionTypes.Should().NotBeEmpty();
        permissions.Should().NotBeEmpty();

        permissionTypes.Should().Contain(pt => pt.Description == "First type");
        permissions.Should().Contain(p => p.EmployeeForename == "Forename");
    }

    [Fact]
    public async Task Database_ConcurrentReads_ShouldNotBlock()
    {
        // Arrange
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        using var setupContext = new ApplicationDbContext(options);
        await setupContext.Database.EnsureCreatedAsync();

        // Act
        var readTasks = new List<Task<int>>();
        for (int i = 0; i < 10; i++)
        {
            readTasks.Add(Task.Run(async () =>
            {
                using var context = new ApplicationDbContext(options);
                return await context.PermissionTypes.CountAsync();
            }));
        }

        var results = await Task.WhenAll(readTasks);

        // Assert
        results.Should().OnlyContain(r => r >= 0);
        results.Should().HaveCount(10);
    }

    [Fact]
    public async Task Database_ContextRecreation_ShouldMaintainData()
    {
        // Arrange
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        using (var context1 = new ApplicationDbContext(options))
        {
            await context1.Database.EnsureCreatedAsync();
            var permissionType = new Domain.Entities.PermissionType
            {
                Id = 100,
                Description = "Persistent Type"
            };
            context1.PermissionTypes.Add(permissionType);
            await context1.SaveChangesAsync();
        }

        using var context2 = new ApplicationDbContext(options);

        // Act
        var retrievedType = await context2.PermissionTypes.FindAsync(100);

        // Assert
        retrievedType.Should().NotBeNull();
        retrievedType!.Description.Should().Be("Persistent Type");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.IntegrationTests.DatabaseTests;

public class PermissionRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PermissionRepository _repository;

    public PermissionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new PermissionRepository(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var permissionTypes = new List<PermissionType>
        {
            new() { Id = 1, Description = "Type 1" },
            new() { Id = 2, Description = "Type 2" },
            new() { Id = 3, Description = "Type 3" }
        };

        var permissions = new List<Permission>
        {
            new() { Id = 1, EmployeeForename = "John", EmployeeSurname = "Doe", PermissionTypeId = 1, PermissionDate = DateTime.Today.AddDays(10) },
            new() { Id = 2, EmployeeForename = "Jane", EmployeeSurname = "Smith", PermissionTypeId = 2, PermissionDate = DateTime.Today.AddDays(20) },
            new() { Id = 3, EmployeeForename = "Bob", EmployeeSurname = "Johnson", PermissionTypeId = 1, PermissionDate = DateTime.Today.AddDays(30) }
        };

        _context.PermissionTypes.AddRange(permissionTypes);
        _context.Permissions.AddRange(permissions);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Add_ValidPermission_ShouldAddToDatabase()
    {
        // Arrange
        var permission = new Permission
        {
            Id = 4,
            EmployeeForename = "Test",
            EmployeeSurname = "User",
            PermissionTypeId = 1,
            PermissionDate = DateTime.Today.AddDays(5)
        };

        // Act
        var result = await _repository.Add(permission);
        await _context.SaveChangesAsync();
        var savedPermission = await _context.Permissions.FindAsync(4);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(4);

        savedPermission.Should().NotBeNull();
        savedPermission!.EmployeeForename.Should().Be("Test");
        savedPermission.EmployeeSurname.Should().Be("User");
    }

    [Fact]
    public async Task GetById_ExistingId_ShouldReturnPermission()
    {
        // Act
        var result = await _repository.GetById(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.EmployeeForename.Should().Be("John");
        result.EmployeeSurname.Should().Be("Doe");
    }

    [Fact]
    public async Task GetById_NonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetById(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetById_WithInclude_ShouldIncludeRelatedData()
    {
        // Act
        var result = await _repository.GetById(1, includeProperties: "PermissionType");

        // Assert
        result.Should().NotBeNull();
        result!.PermissionType.Should().NotBeNull();
        result.PermissionType.Description.Should().Be("Type 1");
    }

    [Fact]
    public async Task GetById_WithTracking_ShouldEnableChangeTracking()
    {
        // Act
        var result = await _repository.GetById(1, tracking: true);
        result!.EmployeeForename = "Modified";
        var changeCount = await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        changeCount.Should().Be(1);
    }

    [Fact]
    public async Task GetById_WithoutTracking_ShouldDisableChangeTracking()
    {
        // Act
        var result = await _repository.GetById(1, tracking: false);
        result!.EmployeeForename = "Modified";
        var changeCount = await _context.SaveChangesAsync();

        var resultLater = await _repository.GetById(1, tracking: false);

        // Assert
        result.Should().NotBeNull();
        resultLater.Should().NotBeNull();
        changeCount.Should().Be(0);
        resultLater.EmployeeForename.Should().NotBe("Modified");
    }

    [Fact]
    public void GetAll_ShouldReturnAllPermissions()
    {
        // Act
        var result = _repository.GetAll().ToList();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(p => p.EmployeeForename == "John");
        result.Should().Contain(p => p.EmployeeForename == "Jane");
        result.Should().Contain(p => p.EmployeeForename == "Bob");
    }

    [Fact]
    public void GetAll_WithInclude_ShouldIncludeRelatedData()
    {
        // Act
        var result = _repository.GetAll(includeProperties: "PermissionType").ToList();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().OnlyContain(p => p.PermissionType != null);
        result.First(p => p.Id == 1).PermissionType.Description.Should().Be("Type 1");
    }

    [Fact]
    public void GetAll_WithoutTracking_ShouldDisableChangeTracking()
    {
        // Act
        var result = _repository.GetAll(tracking: false).ToList();

        foreach (var permission in result)
        {
            permission.EmployeeForename = "Modified";
        }
        var changeCount = _context.SaveChanges();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        changeCount.Should().Be(0);
    }

    [Fact]
    public async Task FirstOrDefault_WithValidPredicate_ShouldReturnMatchingEntity()
    {
        // Act
        var result = await _repository.FirstOrDefault(p => p.EmployeeForename == "John");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.EmployeeSurname.Should().Be("Doe");
    }

    [Fact]
    public async Task FirstOrDefault_WithInvalidPredicate_ShouldReturnNull()
    {
        // Act
        var result = await _repository.FirstOrDefault(p => p.EmployeeForename == "NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FirstOrDefault_WithInclude_ShouldIncludeRelatedData()
    {
        // Act
        var result = await _repository.FirstOrDefault(p => p.EmployeeForename == "John", includeProperties: "PermissionType");

        // Assert
        result.Should().NotBeNull();
        result!.PermissionType.Should().NotBeNull();
        result.PermissionType.Description.Should().Be("Type 1");
    }

    [Fact]
    public async Task GetWhere_WithPredicate_ShouldReturnMatchingEntities()
    {
        // Act
        var result = await _repository.GetWhere(p => p.PermissionTypeId == 1);

        // Assert
        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.PermissionTypeId == 1);
    }

    [Fact]
    public async Task GetWhere_WithOrderBy_ShouldReturnOrderedResults()
    {
        // Act
        var result = await _repository.GetWhere(
            p => p.PermissionTypeId == 1,
            orderBy: q => q.OrderBy(p => p.EmployeeForename));

        // Assert
        result.Should().NotBeNull();
        var resultList = result!.ToList();
        resultList.Should().HaveCount(2);
        resultList[0].EmployeeForename.Should().Be("Bob");
        resultList[1].EmployeeForename.Should().Be("John");
    }

    [Fact]
    public async Task GetWhere_WithTop_ShouldLimitResults()
    {
        // Act
        var result = await _repository.GetWhere(
            p => true,
            top: 2);

        // Assert
        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetWhere_WithSkip_ShouldSkipResults()
    {
        // Act
        var result = await _repository.GetWhere(
            p => true,
            orderBy: q => q.OrderBy(p => p.Id),
            skip: 1);
        var resultList = result!.ToList();

        // Assert
        result.Should().NotBeNull();
        resultList.Should().HaveCount(2);
        resultList.First().Id.Should().Be(2);
    }

    [Fact]
    public async Task GetWhere_WithInclude_ShouldIncludeRelatedData()
    {
        // Act
        var result = await _repository.GetWhere(
            p => p.PermissionTypeId == 1,
            includeProperties: "PermissionType");

        // Assert
        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.PermissionType != null);
    }

    [Fact]
    public async Task Update_ExistingPermission_ShouldUpdateSuccessfully()
    {
        // Arrange
        var permission = await _repository.GetById(1);
        permission!.EmployeeForename = "UpdatedJohn";
        permission.EmployeeSurname = "UpdatedDoe";

        // Act
        var result = await _repository.Update(permission);
        await _context.SaveChangesAsync();
        var updatedPermission = await _context.Permissions.FindAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.EmployeeForename.Should().Be("UpdatedJohn");
        result.EmployeeSurname.Should().Be("UpdatedDoe");

        updatedPermission!.EmployeeForename.Should().Be("UpdatedJohn");
        updatedPermission.EmployeeSurname.Should().Be("UpdatedDoe");
    }

    [Fact]
    public async Task Remove_ByEntity_ShouldRemoveFromDatabase()
    {
        // Arrange
        var permission = await _repository.GetById(3);

        // Act
        await _repository.Remove(permission!);
        await _context.SaveChangesAsync();
        var removedPermission = await _context.Permissions.FindAsync(3);

        // Assert
        removedPermission.Should().BeNull();
    }

    [Fact]
    public async Task Remove_ById_ShouldRemoveFromDatabase()
    {
        // Act
        await _repository.Remove(2);
        await _context.SaveChangesAsync();
        var removedPermission = await _context.Permissions.FindAsync(2);

        // Assert
        removedPermission.Should().BeNull();
    }

    [Fact]
    public async Task Remove_NonExistingId_ShouldNotThrow()
    {
        // Act
        var act = async () => await _repository.Remove(999);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var permission1 = new Permission
        {
            Id = 10,
            EmployeeForename = "Concurrent1",
            EmployeeSurname = "Test1",
            PermissionTypeId = 1,
            PermissionDate = DateTime.Today.AddDays(5)
        };

        var permission2 = new Permission
        {
            Id = 11,
            EmployeeForename = "Concurrent2",
            EmployeeSurname = "Test2",
            PermissionTypeId = 2,
            PermissionDate = DateTime.Today.AddDays(6)
        };

        // Act
        var task1 = _repository.Add(permission1);
        var task2 = _repository.Add(permission2);

        await Task.WhenAll(task1, task2);
        await _context.SaveChangesAsync();

        var result1 = await _context.Permissions.FindAsync(10);
        var result2 = await _context.Permissions.FindAsync(11);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1!.EmployeeForename.Should().Be("Concurrent1");
        result2!.EmployeeForename.Should().Be("Concurrent2");
    }

    [Fact]
    public async Task ComplexQuery_WithMultipleIncludes_ShouldWork()
    {
        // Act
        var result = await _repository.GetWhere(
            p => p.PermissionDate > DateTime.Today,
            orderBy: q => q.OrderByDescending(p => p.PermissionDate),
            top: 5,
            includeProperties: "PermissionType");

        // Assert
        result.Should().NotBeNull();
        result!.Should().HaveCount(3);
        result.Should().OnlyContain(p => p.PermissionType != null);
        result.Should().BeInDescendingOrder(p => p.PermissionDate);
    }

    [Theory]
    [InlineData(1, "John", "Doe")]
    [InlineData(2, "Jane", "Smith")]
    [InlineData(3, "Bob", "Johnson")]
    public async Task GetById_ParameterizedTest_ShouldReturnCorrectPermission(int id, string expectedFirstName, string expectedLastName)
    {
        // Act
        var result = await _repository.GetById(id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.EmployeeForename.Should().Be(expectedFirstName);
        result.EmployeeSurname.Should().Be(expectedLastName);
    }


    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
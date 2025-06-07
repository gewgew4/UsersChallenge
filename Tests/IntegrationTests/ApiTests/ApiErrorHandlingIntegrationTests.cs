using Application.Commands;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Tests.IntegrationTests.ApiTests;

[Collection("Integration")]
public class ApiErrorHandlingIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task RequestPermission_WithInvalidJson_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await TestClient.PostAsync("/api/permissions", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RequestPermission_WithEmptyBody_ShouldReturnBadRequest()
    {
        // Arrange
        var content = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await TestClient.PostAsync("/api/permissions", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RequestPermission_WithNullValues_ShouldReturnInternalServerError()
    {
        // Arrange
        var command = new
        {
            employeeForename = (string?)null,
            employeeSurname = (string?)null,
            permissionTypeId = 0,
            permissionDate = (DateTime?)null
        };

        // Act
        var response = await TestClient.PostAsJsonAsync("/api/permissions", command);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.InternalServerError, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ModifyPermission_WithMismatchedIds_ShouldStillProcess()
    {
        // Arrange
        var createCommand = new RequestPermissionCommand
        {
            EmployeeForename = "Test",
            EmployeeSurname = "User",
            PermissionTypeId = 1,
            PermissionDate = DateTime.Today.AddDays(10)
        };

        var createResponse = await TestClient.PostAsJsonAsync("/api/permissions", createCommand);
        var permissionId = await createResponse.Content.ReadFromJsonAsync<int>();

        var modifyCommand = new ModifyPermissionCommand
        {
            Id = 999,
            EmployeeForename = "Updated",
            EmployeeSurname = "User",
            PermissionTypeId = 2,
            PermissionDate = DateTime.Today.AddDays(20)
        };

        // Act
        var response = await TestClient.PutAsJsonAsync($"/api/permissions/{permissionId}", modifyCommand);
        var getResponse = await TestClient.GetAsync($"/api/permissions/{permissionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPermission_WithNonNumericId_ShouldReturnBadRequest()
    {
        // Act
        var response = await TestClient.GetAsync("/api/permissions/not-a-number");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ModifyPermission_WithNonNumericId_ShouldReturnBadRequest()
    {
        // Arrange
        var modifyCommand = new ModifyPermissionCommand
        {
            EmployeeForename = "Test",
            EmployeeSurname = "User",
            PermissionTypeId = 1,
            PermissionDate = DateTime.Today.AddDays(10)
        };

        // Act
        var response = await TestClient.PutAsJsonAsync("/api/permissions/not-a-number", modifyCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RequestPermission_WithVeryLongNames_ShouldReturnInternalServerError()
    {
        // Arrange
        var command = new RequestPermissionCommand
        {
            EmployeeForename = new string('A', 101),
            EmployeeSurname = new string('B', 101),
            PermissionTypeId = 1,
            PermissionDate = DateTime.Today.AddDays(10)
        };

        // Act
        var response = await TestClient.PostAsJsonAsync("/api/permissions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task RequestPermission_WithPastDate_ShouldReturnInternalServerError()
    {
        // Arrange
        var command = new RequestPermissionCommand
        {
            EmployeeForename = "Test",
            EmployeeSurname = "User",
            PermissionTypeId = 1,
            PermissionDate = DateTime.Today.AddDays(-1)
        };

        // Act
        var response = await TestClient.PostAsJsonAsync("/api/permissions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task RequestPermission_WithInvalidPermissionTypeId_ShouldReturnInternalServerError()
    {
        // Arrange
        var command = new RequestPermissionCommand
        {
            EmployeeForename = "Test",
            EmployeeSurname = "User",
            PermissionTypeId = 0,
            PermissionDate = DateTime.Today.AddDays(10)
        };

        // Act
        var response = await TestClient.PostAsJsonAsync("/api/permissions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ModifyPermission_WithInvalidPermissionTypeId_ShouldReturnInternalServerError()
    {
        // Arrange
        var createCommand = new RequestPermissionCommand
        {
            EmployeeForename = "Test",
            EmployeeSurname = "User",
            PermissionTypeId = 1,
            PermissionDate = DateTime.Today.AddDays(10)
        };

        var createResponse = await TestClient.PostAsJsonAsync("/api/permissions", createCommand);
        var permissionId = await createResponse.Content.ReadFromJsonAsync<int>();

        var modifyCommand = new ModifyPermissionCommand
        {
            Id = permissionId,
            EmployeeForename = "Updated",
            EmployeeSurname = "User",
            PermissionTypeId = -1,
            PermissionDate = DateTime.Today.AddDays(20)
        };

        // Act
        var response = await TestClient.PutAsJsonAsync($"/api/permissions/{permissionId}", modifyCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Api_WithUnsupportedHttpMethod_ShouldReturnMethodNotAllowed()
    {
        // Act
        var response = await TestClient.DeleteAsync("/api/permissions/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Api_WithInvalidRoute_ShouldReturnNotFound()
    {
        // Act
        var response = await TestClient.GetAsync("/api/invalid-route");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RequestPermission_WithMissingContentType_ShouldReturnUnsupportedMediaType()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new RequestPermissionCommand
        {
            EmployeeForename = "Test",
            EmployeeSurname = "User",
            PermissionTypeId = 1,
            PermissionDate = DateTime.Today.AddDays(10)
        });
        var content = new StringContent(json, Encoding.UTF8);

        // Act
        var response = await TestClient.PostAsync("/api/permissions", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task RequestPermission_WithWhitespaceNames_ShouldReturnInternalServerError(string name)
    {
        // Arrange
        var command = new RequestPermissionCommand
        {
            EmployeeForename = name,
            EmployeeSurname = name,
            PermissionTypeId = 1,
            PermissionDate = DateTime.Today.AddDays(10)
        };

        // Act
        var response = await TestClient.PostAsJsonAsync("/api/permissions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ModifyPermission_WithZeroId_ShouldReturnInternalServerError()
    {
        // Arrange
        var modifyCommand = new ModifyPermissionCommand
        {
            Id = 0,
            EmployeeForename = "Test",
            EmployeeSurname = "User",
            PermissionTypeId = 1,
            PermissionDate = DateTime.Today.AddDays(10)
        };

        // Act
        var response = await TestClient.PutAsJsonAsync("/api/permissions/0", modifyCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetPermission_WithZeroId_ShouldReturnInternalServerError()
    {
        // Act
        var response = await TestClient.GetAsync("/api/permissions/0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetPermission_WithNegativeId_ShouldReturnInternalServerError()
    {
        // Act
        var response = await TestClient.GetAsync("/api/permissions/-1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task RequestPermission_WithSpecialCharactersInNames_ShouldCreateSuccessfully()
    {
        // Arrange
        var command = new RequestPermissionCommand
        {
            EmployeeForename = "José-María",
            EmployeeSurname = "O'Connor",
            PermissionTypeId = 1,
            PermissionDate = DateTime.Today.AddDays(10)
        };

        // Act
        var response = await TestClient.PostAsJsonAsync("/api/permissions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var permissionId = await response.Content.ReadFromJsonAsync<int>();
        permissionId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RequestPermission_WithUnicodeCharacters_ShouldCreateSuccessfully()
    {
        // Arrange
        var command = new RequestPermissionCommand
        {
            EmployeeForename = "張三",
            EmployeeSurname = "李四",
            PermissionTypeId = 1,
            PermissionDate = DateTime.Today.AddDays(10)
        };

        // Act
        var response = await TestClient.PostAsJsonAsync("/api/permissions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var permissionId = await response.Content.ReadFromJsonAsync<int>();
        permissionId.Should().BeGreaterThan(0);
    }
}
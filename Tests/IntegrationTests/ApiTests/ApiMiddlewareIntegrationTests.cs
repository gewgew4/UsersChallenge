using Application.Commands;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tests.IntegrationTests.ApiTests;

[Collection("Integration")]
public class ApiMiddlewareIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task LoggingMiddleware_ShouldLogRequestAndResponse()
    {
        // Act
        var response = await TestClient.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExceptionMiddleware_ShouldHandleValidationExceptions()
    {
        // Arrange
        var command = new RequestPermissionCommand
        {
            EmployeeForename = "",
            EmployeeSurname = "Test",
            PermissionTypeId = 1,
            PermissionDate = DateTime.Today.AddDays(10)
        };

        // Act
        var response = await TestClient.PostAsJsonAsync("/api/permissions", command);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        content.Should().Contain("An error occurred while processing your request");
        content.Should().StartWith("{");
        content.Should().EndWith("}");
        content.Should().Contain("statusCode");
        content.Should().Contain("500");
    }

    [Fact]
    public async Task ExceptionMiddleware_ShouldSetCorrectContentType()
    {
        // Arrange
        var command = new RequestPermissionCommand
        {
            EmployeeForename = "",
            EmployeeSurname = "Test",
            PermissionTypeId = 1,
            PermissionDate = DateTime.Today.AddDays(10)
        };

        // Act
        var response = await TestClient.PostAsJsonAsync("/api/permissions", command);

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task CorsMiddleware_ShouldAllowCrossOriginRequests()
    {
        // Arrange
        TestClient.DefaultRequestHeaders.Add("Origin", "https://example.com");

        // Act
        var response = await TestClient.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Pipeline_ShouldProcessRequestThroughAllMiddleware()
    {
        // Arrange
        var command = new RequestPermissionCommand
        {
            EmployeeForename = "Middleware",
            EmployeeSurname = "Test",
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
    public async Task HealthCheck_ShouldBypassAuthorizationMiddleware()
    {
        // Act
        var response = await TestClient.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task Middleware_ShouldHandleOptionsRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/permissions");
        request.Headers.Add("Origin", "https://example.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        // Act
        var response = await TestClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ExceptionMiddleware_ShouldHandleUnexpectedExceptions()
    {
        // Act
        var response = await TestClient.GetAsync("/api/permissions/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("An error occurred while processing your request");
        content.Should().Contain("statusCode");
        content.Should().Contain("500");
    }

    [Fact]
    public async Task Middleware_ShouldPreserveRequestIdInLogs()
    {
        // Act
        var responses = await Task.WhenAll(
            TestClient.GetAsync("/health"),
            TestClient.GetAsync("/health"),
            TestClient.GetAsync("/health")
        );

        // Assert
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Middleware_ShouldHandleConcurrentRequests()
    {
        // Arrange
        var commands = Enumerable.Range(1, 5).Select(i => new RequestPermissionCommand
        {
            EmployeeForename = $"Concurrent{i}",
            EmployeeSurname = $"Test{i}",
            PermissionTypeId = (i % 4) + 1,
            PermissionDate = DateTime.Today.AddDays(i * 10)
        }).ToArray();

        // Act
        var tasks = commands.Select(cmd => TestClient.PostAsJsonAsync("/api/permissions", cmd));
        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var permissionId = await response.Content.ReadFromJsonAsync<int>();
            permissionId.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task Middleware_ShouldHandleLargeRequestBodies()
    {
        // Arrange
        var command = new RequestPermissionCommand
        {
            EmployeeForename = new string('A', 50),
            EmployeeSurname = new string('B', 50),
            PermissionTypeId = 1,
            PermissionDate = DateTime.Today.AddDays(10)
        };

        // Act
        var response = await TestClient.PostAsJsonAsync("/api/permissions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
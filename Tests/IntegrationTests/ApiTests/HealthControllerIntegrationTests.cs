using FluentAssertions;
using System.Net;
using Xunit;

namespace Tests.IntegrationTests.ApiTests;

[Collection("Integration")]
public class HealthControllerIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task GetHealth_WhenAllServicesHealthy_ShouldReturnHealthy()
    {
        // Act
        var response = await TestClient.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealth_MultipleCalls_ShouldAlwaysReturnHealthy()
    {
        // Act
        for (int i = 0; i < 3; i++)
        {
            var response = await TestClient.GetAsync("/health");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().Contain("Healthy");
        }
    }
}
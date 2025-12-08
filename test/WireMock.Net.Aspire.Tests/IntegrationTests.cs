// Copyright © WireMock.Net

using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Projects;
using WireMock.Net.Aspire.Tests.Facts;
using Xunit.Abstractions;

namespace WireMock.Net.Aspire.Tests;

public class IntegrationTests(ITestOutputHelper output)
{
    private record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary);

    [DockerIsRunningInLinuxContainerModeFact]
    public async Task StartAppHostWithWireMockAndCreateHttpClientToCallTheMockedWeatherForecastEndpoint()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<WireMock_Net_Aspire_TestAppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("wiremock-service");

        using var httpClient = app.CreateHttpClient("wiremock-service");

        // Act 1
        var weatherForecasts1 = await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast");

        // Assert 1
        weatherForecasts1.Should().BeEquivalentTo(new[]
        {
            new WeatherForecast(new DateOnly(2024, 5, 24), -10, "Freezing"),
            new WeatherForecast(new DateOnly(2024, 5, 25), +33, "Hot")
        });

        // Act 2
        var weatherForecasts2 = await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast2");

        // Assert 2
        weatherForecasts2.Should().HaveCount(5);
    }

    [DockerIsRunningInLinuxContainerModeFact]
    public async Task StartAppHostWithWireMockAndCreateWireMockAdminClientToCallTheAdminEndpoint()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<WireMock_Net_Aspire_TestAppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("wiremock-service");

        var adminClient = app.CreateWireMockAdminClient("wiremock-service");

        // Act 1
        var settings = await adminClient.GetSettingsAsync();

        // Assert 1
        settings.Should().NotBeNull();

        // Act 2
        var mappings = await adminClient.GetMappingsAsync();

        // Assert 2
        mappings.Should().HaveCount(2);
    }

    #region Assertion Tests

    [DockerIsRunningInLinuxContainerModeFact]
    public async Task ShouldHaveReceived_WithMatchingRequest_ShouldNotThrow()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<WireMock_Net_Aspire_TestAppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("wiremock-service");

        using var httpClient = app.CreateHttpClient("wiremock-service");

        // Act - Make a request first
        await httpClient.GetAsync("/weatherforecast");

        // Assert - Verify the request was received
        await app.ShouldHaveReceivedGet("wiremock-service", "/weatherforecast")
            .ExecuteAsync();
    }

    [DockerIsRunningInLinuxContainerModeFact]
    public async Task ShouldHaveReceived_WithNonMatchingRequest_ShouldThrow()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<WireMock_Net_Aspire_TestAppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("wiremock-service");

        // Act & Assert - No request was made to /nonexistent, so this should throw
        Func<Task> act = async () => await app
            .ShouldHaveReceivedGet("wiremock-service", "/nonexistent")
            .ExecuteAsync();

        await act.Should().ThrowAsync<WireMockAssertionException>()
            .WithMessage("*at least one request*");
    }

    [DockerIsRunningInLinuxContainerModeFact]
    public async Task ShouldHaveReceived_WithTimes_ShouldVerifyExactCount()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<WireMock_Net_Aspire_TestAppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("wiremock-service");

        using var httpClient = app.CreateHttpClient("wiremock-service");

        // Act - Make 2 requests
        await httpClient.GetAsync("/weatherforecast");
        await httpClient.GetAsync("/weatherforecast");

        // Assert - Verify exactly 2 requests were received
        await app.ShouldHaveReceivedGet("wiremock-service", "/weatherforecast")
            .Times(2)
            .ExecuteAsync();
    }

    [DockerIsRunningInLinuxContainerModeFact]
    public async Task ShouldHaveReceived_WithWrongCount_ShouldThrow()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<WireMock_Net_Aspire_TestAppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("wiremock-service");

        using var httpClient = app.CreateHttpClient("wiremock-service");

        // Act - Make 1 request
        await httpClient.GetAsync("/weatherforecast");

        // Assert - Should throw because we expect 5 but only made 1
        Func<Task> act = async () => await app
            .ShouldHaveReceivedGet("wiremock-service", "/weatherforecast")
            .Times(5)
            .ExecuteAsync();

        await act.Should().ThrowAsync<WireMockAssertionException>()
            .WithMessage("*exactly 5*but found 1*");
    }

    [DockerIsRunningInLinuxContainerModeFact]
    public async Task ShouldHaveReceivedNoRequests_WithNoRequests_ShouldNotThrow()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<WireMock_Net_Aspire_TestAppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("wiremock-service");

        // Reset any existing requests first
        await app.ResetRequestsAsync("wiremock-service");

        // Act & Assert - No requests should have been made after reset
        await app.ShouldHaveReceivedNoRequestsAsync("wiremock-service");
    }

    [DockerIsRunningInLinuxContainerModeFact]
    public async Task GetAllRequestsAsync_ShouldReturnAllRequests()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<WireMock_Net_Aspire_TestAppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("wiremock-service");

        // Reset and make fresh requests
        await app.ResetRequestsAsync("wiremock-service");

        using var httpClient = app.CreateHttpClient("wiremock-service");
        await httpClient.GetAsync("/weatherforecast");
        await httpClient.GetAsync("/weatherforecast2");

        // Act
        var requests = await app.GetAllRequestsAsync("wiremock-service");

        // Assert
        requests.Should().HaveCount(2);
        requests.Select(r => r.Request?.Path).Should().Contain("/weatherforecast");
        requests.Select(r => r.Request?.Path).Should().Contain("/weatherforecast2");
    }

    [DockerIsRunningInLinuxContainerModeFact]
    public async Task ResetRequestsAsync_ShouldClearAllRequests()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<WireMock_Net_Aspire_TestAppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("wiremock-service");

        using var httpClient = app.CreateHttpClient("wiremock-service");
        await httpClient.GetAsync("/weatherforecast");

        // Act
        await app.ResetRequestsAsync("wiremock-service");

        // Assert
        var requests = await app.GetAllRequestsAsync("wiremock-service");
        requests.Should().BeEmpty();
    }

    [DockerIsRunningInLinuxContainerModeFact]
    public async Task FluentBuilder_WithMethodAndPath_ShouldFilterCorrectly()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<WireMock_Net_Aspire_TestAppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("wiremock-service");

        await app.ResetRequestsAsync("wiremock-service");

        using var httpClient = app.CreateHttpClient("wiremock-service");
        await httpClient.GetAsync("/weatherforecast");

        // Act & Assert - Using full fluent builder
        await app.ShouldHaveReceived("wiremock-service")
            .WithMethod("GET")
            .WithPath("/weatherforecast")
            .AtLeastOnce()
            .ExecuteAsync();
    }

    #endregion

    #region OpenAPI Tests

    [DockerIsRunningInLinuxContainerModeFact]
    public async Task WithOpenApiDocument_ShouldGenerateMappingsFromSpec()
    {
        // Arrange - Use the TestAppHost which has a "wiremock-openapi" resource configured with OpenAPI
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<WireMock_Net_Aspire_TestAppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("wiremock-openapi");

        using var httpClient = app.CreateHttpClient("wiremock-openapi");

        // Act - Call the endpoint generated from OpenAPI spec
        var response = await httpClient.GetAsync("/api/pets");

        // Assert - The endpoint should return 200 (mapping was created from OpenAPI spec)
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Verify mappings were created via admin API
        var adminClient = app.CreateWireMockAdminClient("wiremock-openapi");
        var mappings = await adminClient.GetMappingsAsync();

        // Should have at least 1 mapping (for /api/pets GET)
        mappings.Should().HaveCountGreaterOrEqualTo(1);
    }

    #endregion
}
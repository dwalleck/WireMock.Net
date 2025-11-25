// Copyright © WireMock.Net
//
// This test file demonstrates the enhanced WireMock.Net.Aspire assertion features:
// 1. Fluent Request Verification API
// 2. Convenience Methods for Common Assertions
// 3. Request Inspection and Reset Utilities

using System.Net.Http.Json;

namespace WireMock.Net.Aspire.EnhancedFeatures.Tests;

/// <summary>
/// Examples demonstrating the WireMock.Net.Aspire fluent assertion API.
/// These tests require Docker to be running in Linux container mode.
/// </summary>
public class AssertionExamplesTests
{
    // ==========================================================================
    // EXAMPLE 1: Basic Request Verification
    // ==========================================================================

    /// <summary>
    /// Demonstrates basic GET request verification using the fluent API.
    /// </summary>
    [Fact(Skip = "Requires Docker - remove Skip to run")]
    public async Task Example1_VerifyGetRequest_UsingFluentApi()
    {
        // Arrange - Start the Aspire app host
        var appHostBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WireMock_Net_Aspire_EnhancedFeatures_AppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();

        // Wait for WireMock to be healthy
        await app.ResourceNotifications.WaitForResourceHealthyAsync("basic-api");

        // Create HTTP client to make requests
        using var httpClient = app.CreateHttpClient("basic-api");

        // Act - Make a request to the mocked API
        var response = await httpClient.GetAsync("/api/health");

        // Assert - Verify the request was received using the fluent API
        await app.ShouldHaveReceivedGet("basic-api", "/api/health")
            .ExecuteAsync();

        // Also verify the response
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    // ==========================================================================
    // EXAMPLE 2: Verify Request Count
    // ==========================================================================

    /// <summary>
    /// Demonstrates verifying exact request counts.
    /// </summary>
    [Fact(Skip = "Requires Docker - remove Skip to run")]
    public async Task Example2_VerifyExactRequestCount()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WireMock_Net_Aspire_EnhancedFeatures_AppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("basic-api");

        using var httpClient = app.CreateHttpClient("basic-api");

        // Reset requests to start fresh
        await app.ResetRequestsAsync("basic-api");

        // Act - Make exactly 3 requests
        await httpClient.GetAsync("/api/users");
        await httpClient.GetAsync("/api/users");
        await httpClient.GetAsync("/api/users");

        // Assert - Verify exactly 3 requests were received
        await app.ShouldHaveReceivedGet("basic-api", "/api/users")
            .Times(3)
            .ExecuteAsync();
    }

    // ==========================================================================
    // EXAMPLE 3: Verify POST Request with Body
    // ==========================================================================

    /// <summary>
    /// Demonstrates verifying POST requests with specific content.
    /// </summary>
    [Fact(Skip = "Requires Docker - remove Skip to run")]
    public async Task Example3_VerifyPostRequest_WithBodyContent()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WireMock_Net_Aspire_EnhancedFeatures_AppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("basic-api");

        using var httpClient = app.CreateHttpClient("basic-api");
        await app.ResetRequestsAsync("basic-api");

        // Act - Make a POST request with JSON body
        var newUser = new { Name = "Charlie", Email = "charlie@example.com" };
        await httpClient.PostAsJsonAsync("/api/users", newUser);

        // Assert - Verify the POST request was received with expected body content
        await app.ShouldHaveReceivedPost("basic-api", "/api/users")
            .WithBodyContaining("Charlie")
            .WithBodyContaining("charlie@example.com")
            .ExecuteAsync();
    }

    // ==========================================================================
    // EXAMPLE 4: Verify Request Headers
    // ==========================================================================

    /// <summary>
    /// Demonstrates verifying requests with specific headers.
    /// </summary>
    [Fact(Skip = "Requires Docker - remove Skip to run")]
    public async Task Example4_VerifyRequest_WithHeaders()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WireMock_Net_Aspire_EnhancedFeatures_AppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("basic-api");

        using var httpClient = app.CreateHttpClient("basic-api");
        await app.ResetRequestsAsync("basic-api");

        // Act - Make a request with custom headers
        httpClient.DefaultRequestHeaders.Add("X-Custom-Header", "CustomValue");
        httpClient.DefaultRequestHeaders.Add("X-Request-Id", "req-12345");
        await httpClient.GetAsync("/api/users");

        // Assert - Verify the request had the expected headers
        await app.ShouldHaveReceived("basic-api")
            .WithMethod("GET")
            .WithPath("/api/users")
            .WithHeader("X-Custom-Header", "CustomValue")
            .WithHeader("X-Request-Id", "req-12345")
            .ExecuteAsync();
    }

    // ==========================================================================
    // EXAMPLE 5: Verify No Requests Made
    // ==========================================================================

    /// <summary>
    /// Demonstrates verifying that no requests were made.
    /// Useful for ensuring certain API calls are NOT happening.
    /// </summary>
    [Fact(Skip = "Requires Docker - remove Skip to run")]
    public async Task Example5_VerifyNoRequestsMade()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WireMock_Net_Aspire_EnhancedFeatures_AppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("basic-api");

        // Reset all requests
        await app.ResetRequestsAsync("basic-api");

        // Act - Don't make any requests

        // Assert - Verify no requests were made
        await app.ShouldHaveReceivedNoRequestsAsync("basic-api");
    }

    // ==========================================================================
    // EXAMPLE 6: Full Fluent Builder
    // ==========================================================================

    /// <summary>
    /// Demonstrates the full fluent builder API for complex assertions.
    /// </summary>
    [Fact(Skip = "Requires Docker - remove Skip to run")]
    public async Task Example6_FullFluentBuilder_ComplexAssertion()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WireMock_Net_Aspire_EnhancedFeatures_AppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("full-api");

        using var httpClient = app.CreateHttpClient("full-api");
        await app.ResetRequestsAsync("full-api");

        // Act - Make multiple different requests
        await httpClient.GetAsync("/api/products");
        await httpClient.PostAsJsonAsync("/api/products", new { Name = "NewProduct", Price = 49.99 });

        // Assert - Use full fluent builder for complex verification
        await app.ShouldHaveReceived("full-api")
            .WithMethod("GET")
            .WithPath("/api/products")
            .AtLeastOnce()
            .ExecuteAsync();

        await app.ShouldHaveReceived("full-api")
            .WithMethod("POST")
            .WithPath("/api/products")
            .WithHeader("Content-Type", "application/json")
            .WithBodyContaining("NewProduct")
            .Times(1)
            .ExecuteAsync();
    }

    // ==========================================================================
    // EXAMPLE 7: Get All Requests for Custom Inspection
    // ==========================================================================

    /// <summary>
    /// Demonstrates retrieving all requests for custom inspection.
    /// </summary>
    [Fact(Skip = "Requires Docker - remove Skip to run")]
    public async Task Example7_GetAllRequests_ForCustomInspection()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WireMock_Net_Aspire_EnhancedFeatures_AppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("basic-api");

        using var httpClient = app.CreateHttpClient("basic-api");
        await app.ResetRequestsAsync("basic-api");

        // Act - Make various requests
        await httpClient.GetAsync("/api/health");
        await httpClient.GetAsync("/api/users");
        await httpClient.PostAsJsonAsync("/api/users", new { Name = "Test" });

        // Assert - Get all requests and perform custom inspection
        var allRequests = await app.GetAllRequestsAsync("basic-api");

        allRequests.Should().HaveCount(3);

        // Custom assertions on the request data
        var getPaths = allRequests
            .Where(r => r.Request?.Method == "GET")
            .Select(r => r.Request?.Path)
            .ToList();

        getPaths.Should().Contain("/api/health");
        getPaths.Should().Contain("/api/users");

        var postRequests = allRequests.Where(r => r.Request?.Method == "POST").ToList();
        postRequests.Should().HaveCount(1);
        postRequests[0].Request?.Path.Should().Be("/api/users");
    }

    // ==========================================================================
    // EXAMPLE 8: Assertion Failure Messages
    // ==========================================================================

    /// <summary>
    /// Demonstrates that assertion failures provide helpful error messages.
    /// </summary>
    [Fact(Skip = "Requires Docker - remove Skip to run")]
    public async Task Example8_AssertionFailure_ProvidesHelpfulMessage()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WireMock_Net_Aspire_EnhancedFeatures_AppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("basic-api");

        await app.ResetRequestsAsync("basic-api");

        // Act & Assert - Verify that assertion failure gives a clear message
        Func<Task> act = async () => await app
            .ShouldHaveReceivedGet("basic-api", "/api/nonexistent")
            .ExecuteAsync();

        await act.Should()
            .ThrowAsync<WireMockAssertionException>()
            .WithMessage("*at least one request*GET*/api/nonexistent*");
    }

    // ==========================================================================
    // EXAMPLE 9: Testing Multiple Mock Services
    // ==========================================================================

    /// <summary>
    /// Demonstrates testing multiple WireMock services independently.
    /// </summary>
    [Fact(Skip = "Requires Docker - remove Skip to run")]
    public async Task Example9_TestMultipleMockServices()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WireMock_Net_Aspire_EnhancedFeatures_AppHost>();
        await using var app = await appHostBuilder.BuildAsync();
        await app.StartAsync();

        // Wait for multiple services
        await app.ResourceNotifications.WaitForResourceHealthyAsync("basic-api");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("full-api");

        using var basicClient = app.CreateHttpClient("basic-api");
        using var fullClient = app.CreateHttpClient("full-api");

        // Reset both services
        await app.ResetRequestsAsync("basic-api");
        await app.ResetRequestsAsync("full-api");

        // Act - Make requests to different services
        await basicClient.GetAsync("/api/health");
        await fullClient.GetAsync("/api/products");

        // Assert - Verify requests on each service independently
        await app.ShouldHaveReceivedGet("basic-api", "/api/health")
            .ExecuteAsync();

        await app.ShouldHaveReceivedGet("full-api", "/api/products")
            .ExecuteAsync();

        // Verify basic-api did NOT receive the products request
        await app.ShouldHaveReceived("basic-api")
            .WithPath("/api/products")
            .Times(0)
            .ExecuteAsync();
    }
}

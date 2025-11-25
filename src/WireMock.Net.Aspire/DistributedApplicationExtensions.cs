// Copyright © WireMock.Net

using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Aspire.Hosting.ApplicationModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RestEase;
using WireMock.Admin.Requests;
using WireMock.Client;

// ReSharper disable once CheckNamespace
namespace Aspire.Hosting;

/// <summary>
/// Some WireMock.Net extension methods for working with <see cref="DistributedApplication"/>.
/// Based on https://github.com/dotnet/aspire/blob/main/src/Aspire.Hosting.Testing/DistributedApplicationHostingTestingExtensions.cs
/// </summary>
public static class DistributedApplicationExtensions
{
    /// <summary>
    /// Create a RestEase Admin client which can be used to call the admin REST endpoint.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication"/>.</param>
    /// <param name="resourceName">The resourceName of the resource.</param>
    /// <param name="endpointName">The resourceName of the endpoint on the resource to communicate with.</param>
    /// <returns>A <see cref="IWireMockAdminApi"/></returns>
    public static IWireMockAdminApi CreateWireMockAdminClient(this DistributedApplication app, string resourceName, string? endpointName = default)
    {
        ThrowIfNotStarted(app);

        var (resource, endpointUri) = GetResourceAndEndpointUri(app, resourceName);

        var api = RestClient.For<IWireMockAdminApi>(endpointUri);
        if (resource.Arguments.HasBasicAuthentication)
        {
            api.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{resource.Arguments.AdminUsername}:{resource.Arguments.AdminPassword}")));
        }

        return api;
    }

    private static (WireMockServerResource WireMockServerResource, string EndpointUri) GetResourceAndEndpointUri(IHost app, string resourceName, string? endpointName = default)
    {
        var wireMockServerResource = GetWireMockServerResource(app, resourceName);

        EndpointReference? endpoint;
        if (!string.IsNullOrEmpty(endpointName))
        {
            endpoint = GetEndpointOrDefault(wireMockServerResource, endpointName);
        }
        else
        {
            endpoint = GetEndpointOrDefault(wireMockServerResource, "http") ?? GetEndpointOrDefault(wireMockServerResource, "https");
        }

        if (endpoint is null)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Endpoint '{0}' for resource '{1}' not found.", endpointName, resourceName), nameof(endpointName));
        }

        return (wireMockServerResource, endpoint.Url);
    }

    private static WireMockServerResource GetWireMockServerResource(IHost app, string resourceName)
    {
        var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        try
        {
            var resource = applicationModel.Resources
                .OfType<WireMockServerResource>()
                .Single(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));

            return resource;
        }
        catch (InvalidOperationException ex)
        {
            // Single() throws InvalidOperationException for both "no elements" and "more than one element"
            // Provide a more helpful error message
            var matchingCount = applicationModel.Resources
                .OfType<WireMockServerResource>()
                .Count(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));

            if (matchingCount == 0)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "WireMockServerResource with name '{0}' not found.", resourceName),
                    nameof(resourceName),
                    ex);
            }
            else
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "Multiple WireMockServerResource instances with name '{0}' found. Resource names must be unique.", resourceName),
                    nameof(resourceName),
                    ex);
            }
        }
    }

    private static EndpointReference? GetEndpointOrDefault(IResourceWithEndpoints wireMockServerResource, string endpointName)
    {
        var reference = wireMockServerResource.GetEndpoint(endpointName);

        return reference.IsAllocated ? reference : null;
    }

    private static void ThrowIfNotStarted(IHost app)
    {
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        if (!lifetime.ApplicationStarted.IsCancellationRequested)
        {
            throw new InvalidOperationException("The application must be started before resolving endpoints or connection strings");
        }
    }

    #region Assertions

    /// <summary>
    /// Creates an assertion context for verifying requests made to the WireMock server.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication"/>.</param>
    /// <param name="resourceName">The name of the WireMock resource.</param>
    /// <returns>A <see cref="WireMockAssertionContext"/> for building assertions.</returns>
    public static WireMockAssertionContext CreateAssertions(this DistributedApplication app, string resourceName)
    {
        ThrowIfNotStarted(app);

        var (resource, _) = GetResourceAndEndpointUri(app, resourceName);
        return new WireMockAssertionContext(resource.AdminApi.Value, resourceName);
    }

    /// <summary>
    /// Starts a fluent assertion builder to verify that a request was received.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication"/>.</param>
    /// <param name="resourceName">The name of the WireMock resource.</param>
    /// <returns>A <see cref="RequestVerificationBuilder"/> for building the assertion.</returns>
    public static RequestVerificationBuilder ShouldHaveReceived(this DistributedApplication app, string resourceName)
    {
        var context = app.CreateAssertions(resourceName);
        return new RequestVerificationBuilder(context);
    }

    /// <summary>
    /// Verifies that a GET request was received at the specified path.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication"/>.</param>
    /// <param name="resourceName">The name of the WireMock resource.</param>
    /// <param name="path">The expected request path.</param>
    /// <returns>A <see cref="RequestVerificationBuilder"/> for further configuration.</returns>
    public static RequestVerificationBuilder ShouldHaveReceivedGet(this DistributedApplication app, string resourceName, string path)
    {
        return app.ShouldHaveReceived(resourceName)
            .WithMethod(HttpMethods.Get)
            .WithPath(path);
    }

    /// <summary>
    /// Verifies that a POST request was received at the specified path.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication"/>.</param>
    /// <param name="resourceName">The name of the WireMock resource.</param>
    /// <param name="path">The expected request path.</param>
    /// <returns>A <see cref="RequestVerificationBuilder"/> for further configuration.</returns>
    public static RequestVerificationBuilder ShouldHaveReceivedPost(this DistributedApplication app, string resourceName, string path)
    {
        return app.ShouldHaveReceived(resourceName)
            .WithMethod(HttpMethods.Post)
            .WithPath(path);
    }

    /// <summary>
    /// Verifies that a PUT request was received at the specified path.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication"/>.</param>
    /// <param name="resourceName">The name of the WireMock resource.</param>
    /// <param name="path">The expected request path.</param>
    /// <returns>A <see cref="RequestVerificationBuilder"/> for further configuration.</returns>
    public static RequestVerificationBuilder ShouldHaveReceivedPut(this DistributedApplication app, string resourceName, string path)
    {
        return app.ShouldHaveReceived(resourceName)
            .WithMethod(HttpMethods.Put)
            .WithPath(path);
    }

    /// <summary>
    /// Verifies that a DELETE request was received at the specified path.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication"/>.</param>
    /// <param name="resourceName">The name of the WireMock resource.</param>
    /// <param name="path">The expected request path.</param>
    /// <returns>A <see cref="RequestVerificationBuilder"/> for further configuration.</returns>
    public static RequestVerificationBuilder ShouldHaveReceivedDelete(this DistributedApplication app, string resourceName, string path)
    {
        return app.ShouldHaveReceived(resourceName)
            .WithMethod(HttpMethods.Delete)
            .WithPath(path);
    }

    /// <summary>
    /// Verifies that no requests were received by the WireMock server.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication"/>.</param>
    /// <param name="resourceName">The name of the WireMock resource.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="WireMockAssertionException">Thrown when requests were found.</exception>
    public static async Task ShouldHaveReceivedNoRequestsAsync(this DistributedApplication app, string resourceName, CancellationToken cancellationToken = default)
    {
        await app.ShouldHaveReceived(resourceName)
            .Times(0)
            .ExecuteAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all requests received by the WireMock server.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication"/>.</param>
    /// <param name="resourceName">The name of the WireMock resource.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A collection of request log entries.</returns>
    public static async Task<IReadOnlyList<LogEntryModel>> GetAllRequestsAsync(
        this DistributedApplication app,
        string resourceName,
        CancellationToken cancellationToken = default)
    {
        var context = app.CreateAssertions(resourceName);
        var requests = await context.AdminApi.GetRequestsAsync(cancellationToken);
        return requests.ToList().AsReadOnly();
    }

    /// <summary>
    /// Resets all recorded requests on the WireMock server.
    /// </summary>
    /// <param name="app">The <see cref="DistributedApplication"/>.</param>
    /// <param name="resourceName">The name of the WireMock resource.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task ResetRequestsAsync(this DistributedApplication app, string resourceName, CancellationToken cancellationToken = default)
    {
        var context = app.CreateAssertions(resourceName);
        await context.AdminApi.DeleteRequestsAsync(cancellationToken);
    }

    #endregion
}
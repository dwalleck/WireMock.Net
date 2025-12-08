// Copyright © WireMock.Net

using WireMock.Client;

// ReSharper disable once CheckNamespace
namespace Aspire.Hosting;

/// <summary>
/// Context for fluent WireMock assertions.
/// Provides access to the admin API and enables chaining of assertion methods.
/// </summary>
public class WireMockAssertionContext
{
    /// <summary>
    /// Gets the WireMock admin API client.
    /// </summary>
    internal IWireMockAdminApi AdminApi { get; }

    /// <summary>
    /// Gets the resource name.
    /// </summary>
    public string ResourceName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WireMockAssertionContext"/> class.
    /// </summary>
    /// <param name="adminApi">The WireMock admin API client.</param>
    /// <param name="resourceName">The resource name.</param>
    internal WireMockAssertionContext(IWireMockAdminApi adminApi, string resourceName)
    {
        AdminApi = adminApi;
        ResourceName = resourceName;
    }
}

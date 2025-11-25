// Copyright © WireMock.Net

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace WireMock.Net.Aspire;

internal class WireMockServerLifecycleHook(ILoggerFactory loggerFactory) : IDistributedApplicationLifecycleHook, IAsyncDisposable
{
    private readonly CancellationTokenSource _shutdownCts = new();

    private CancellationTokenSource? _linkedCts;
    private Task? _mappingTask;

    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_shutdownCts.Token, cancellationToken);

        _mappingTask = Task.Run(async () =>
        {
            var wireMockServerResources = appModel.Resources
                .OfType<WireMockServerResource>()
                .ToArray();

            foreach (var wireMockServerResource in wireMockServerResources)
            {
                var logger = loggerFactory.CreateLogger<WireMockServerResource>();
                wireMockServerResource.SetLogger(logger);

                var endpoint = wireMockServerResource.GetEndpoint();
                System.Diagnostics.Debug.Assert(endpoint.IsAllocated);

                await wireMockServerResource.WaitForHealthAsync(_linkedCts.Token);

                try
                {
                    await wireMockServerResource.LoadOpenApiDocumentAsync(_linkedCts.Token);
                }
                catch (Exception ex)
                {
                    if (wireMockServerResource.Arguments.ThrowOnOpenApiLoadFailure)
                    {
                        logger.LogCritical(ex, "Failed to load OpenAPI document for WireMock resource '{ResourceName}'. Startup will fail due to ThrowOnOpenApiLoadFailure setting.", wireMockServerResource.Name);
                        throw;
                    }

                    logger.LogError(ex, "Failed to load OpenAPI document for WireMock resource '{ResourceName}'. The WireMock server will start without OpenAPI mappings.", wireMockServerResource.Name);
                }

                await wireMockServerResource.CallApiMappingBuilderActionAsync(_linkedCts.Token);

                wireMockServerResource.StartWatchingStaticMappings(_linkedCts.Token);
            }
        }, _linkedCts.Token);

        return _mappingTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _shutdownCts.CancelAsync();

        _linkedCts?.Dispose();
        _shutdownCts.Dispose();

        if (_mappingTask is not null)
        {
            await _mappingTask;
        }
    }
}
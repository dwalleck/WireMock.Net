// Copyright © WireMock.Net

using Microsoft.Extensions.Logging;
using RestEase;
using Stef.Validation;
using WireMock.Client;
using WireMock.Client.Extensions;
using WireMock.Net.Aspire;
using WireMock.Util;

// ReSharper disable once CheckNamespace
namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a WireMock.Net Server.
/// </summary>
public class WireMockServerResource : ContainerResource, IResourceWithServiceDiscovery
{
    private const int EnhancedFileSystemWatcherTimeoutMs = 2000;

    internal WireMockServerArguments Arguments { get; }
    internal Lazy<IWireMockAdminApi> AdminApi => _adminApi;
    internal WireMockMappingState ApiMappingState { get; set; } = WireMockMappingState.NoMappings;

    private readonly Lazy<IWireMockAdminApi> _adminApi;
    private ILogger? _logger;
    private EnhancedFileSystemWatcher? _enhancedFileSystemWatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="WireMockServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="arguments">The arguments to start the WireMock.Net Server.</param>
    public WireMockServerResource(string name, WireMockServerArguments arguments) : base(name)
    {
        Arguments = Guard.NotNull(arguments);
        _adminApi = new Lazy<IWireMockAdminApi>(CreateWireMockAdminApi);
    }

    /// <summary>
    /// Gets the HTTP endpoint reference.
    /// </summary>
    /// <returns>An <see cref="EndpointReference"/> object representing the HTTP endpoint reference.</returns>
    public EndpointReference GetEndpoint()
    {
        return new EndpointReference(this, "http");
    }

    /// <summary>
    /// Gets the HTTPS endpoint reference.
    /// </summary>
    /// <returns>An <see cref="EndpointReference"/> object representing the HTTPS endpoint reference.</returns>
    /// <remarks>This endpoint is only available if HTTPS was configured using WithHttpsEndpoint().</remarks>
    public EndpointReference GetHttpsEndpoint()
    {
        return new EndpointReference(this, "https");
    }

    internal void SetLogger(ILogger logger)
    {
        _logger = logger;
    }

    internal async Task WaitForHealthAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Checking Health status from WireMock.Net");
        await AdminApi.Value.WaitForHealthAsync(cancellationToken: cancellationToken);
    }

    internal async Task CallApiMappingBuilderActionAsync(CancellationToken cancellationToken)
    {
        if (Arguments.ApiMappingBuilder == null)
        {
            return;
        }

        _logger?.LogInformation("Calling ApiMappingBuilder to add mappings to WireMock.Net");

        var mappingBuilder = AdminApi.Value.GetMappingBuilder();
        await Arguments.ApiMappingBuilder.Invoke(mappingBuilder, cancellationToken);

        ApiMappingState = WireMockMappingState.Submitted;
    }

    internal async Task LoadOpenApiDocumentAsync(CancellationToken cancellationToken)
    {
        if (!Arguments.HasOpenApiConfiguration)
        {
            return;
        }

        string? content = null;

        if (!string.IsNullOrEmpty(Arguments.OpenApiFilePath))
        {
            if (!File.Exists(Arguments.OpenApiFilePath))
            {
                throw new FileNotFoundException(
                    $"OpenAPI specification file not found: {Arguments.OpenApiFilePath}. " +
                    "Ensure the file path is correct and the file exists.",
                    Arguments.OpenApiFilePath);
            }

            _logger?.LogInformation("Loading OpenAPI spec from file: '{Path}'", Arguments.OpenApiFilePath);
            content = await File.ReadAllTextAsync(Arguments.OpenApiFilePath, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(Arguments.OpenApiDocument))
        {
            _logger?.LogInformation("Loading OpenAPI spec from inline content");
            content = Arguments.OpenApiDocument;
        }
        else if (Arguments.OpenApiDocumentFactoryAsync != null)
        {
            _logger?.LogInformation("Loading OpenAPI spec from async factory");
            content = await Arguments.OpenApiDocumentFactoryAsync();
        }
        else if (Arguments.OpenApiDocumentFactory != null)
        {
            _logger?.LogInformation("Loading OpenAPI spec from factory");
            content = Arguments.OpenApiDocumentFactory();
        }

        if (!string.IsNullOrEmpty(content))
        {
            await AdminApi.Value.OpenApiSaveAsync(content, cancellationToken);
            _logger?.LogInformation("OpenAPI spec loaded successfully");
        }
    }

    internal void StartWatchingStaticMappings(CancellationToken cancellationToken)
    {
        if (!Arguments.WatchStaticMappings || string.IsNullOrEmpty(Arguments.MappingsPath))
        {
            return;
        }

        cancellationToken.Register(() =>
        {
            if (_enhancedFileSystemWatcher != null)
            {
                _enhancedFileSystemWatcher.EnableRaisingEvents = false;
                _enhancedFileSystemWatcher.Created -= FileCreatedChangedOrDeleted;
                _enhancedFileSystemWatcher.Changed -= FileCreatedChangedOrDeleted;
                _enhancedFileSystemWatcher.Deleted -= FileCreatedChangedOrDeleted;

                _enhancedFileSystemWatcher.Dispose();
                _enhancedFileSystemWatcher = null;
            }
        });

        _logger?.LogInformation("Starting to watch static mappings on path: '{Path}'. ", Arguments.MappingsPath);

        _enhancedFileSystemWatcher = new EnhancedFileSystemWatcher(Arguments.MappingsPath, "*.json", EnhancedFileSystemWatcherTimeoutMs)
        {
            IncludeSubdirectories = true
        };
        _enhancedFileSystemWatcher.Created += FileCreatedChangedOrDeleted;
        _enhancedFileSystemWatcher.Changed += FileCreatedChangedOrDeleted;
        _enhancedFileSystemWatcher.Deleted += FileCreatedChangedOrDeleted;
        _enhancedFileSystemWatcher.EnableRaisingEvents = true;
    }

    private IWireMockAdminApi CreateWireMockAdminApi()
    {
        var adminApi = RestClient.For<IWireMockAdminApi>(GetEndpoint().Url);
        return Arguments.HasBasicAuthentication ?
            adminApi.WithAuthorization(Arguments.AdminUsername!, Arguments.AdminPassword!) :
            adminApi;
    }

    private void FileCreatedChangedOrDeleted(object sender, FileSystemEventArgs args)
    {
        // Fire and forget pattern with proper exception handling
        // Using Task.Run to avoid async void and ensure exceptions are observed
        _ = Task.Run(async () =>
        {
            try
            {
                _logger?.LogInformation("MappingFile created, changed or deleted: '{0}'. Triggering ReloadStaticMappings.", args.FullPath);
                await AdminApi.Value.ReloadStaticMappingsAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reloading static mappings after file change: {FilePath}", args.FullPath);
            }
        });
    }
}
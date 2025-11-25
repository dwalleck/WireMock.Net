// Copyright © WireMock.Net

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.WireMock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Stef.Validation;
using WireMock.Client.Builders;
using WireMock.Net.Aspire;

// ReSharper disable once CheckNamespace
namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding WireMock.Net Server resources to the application model.
/// </summary>
public static class WireMockServerBuilderExtensions
{
    // Linux only (https://github.com/dotnet/aspire/issues/854)
    private const string DefaultLinuxImage = "sheyenrath/wiremock.net-alpine";
    private const string DefaultLinuxMappingsPath = "/app/__admin/mappings";

    /// <summary>
    /// Adds a WireMock.Net Server resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The HTTP port for the WireMock Server.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> AddWireMock(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        Guard.NotNull(builder);
        Guard.NotNullOrWhiteSpace(name);
        Guard.Condition(port, p => p is null or > 0 and <= ushort.MaxValue);

        return builder.AddWireMock(name, callback =>
        {
            callback.HttpPort = port;
        });
    }

    /// <summary>
    /// Adds a WireMock.Net Server resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="arguments">The arguments to start the WireMock.Net Server.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> AddWireMock(this IDistributedApplicationBuilder builder, string name, WireMockServerArguments arguments)
    {
        Guard.NotNull(builder);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(arguments);

        var wireMockContainerResource = new WireMockServerResource(name, arguments);

        var healthCheckKey = $"{name}_check";
        var healthCheckRegistration = new HealthCheckRegistration(
            healthCheckKey,
            _ => new WireMockHealthCheck(wireMockContainerResource),
            failureStatus: null,
            tags: null);
        builder.Services.AddHealthChecks().Add(healthCheckRegistration);

        var resourceBuilder = builder
            .AddResource(wireMockContainerResource)
            .WithImage(DefaultLinuxImage)
            .WithEnvironment(ctx => ctx.EnvironmentVariables.Add("DOTNET_USE_POLLING_FILE_WATCHER", "1")) // https://khalidabuhakmeh.com/aspnet-docker-gotchas-and-workarounds#configuration-reloads-and-filesystemwatcher
            .WithHttpEndpoint(port: arguments.HttpPort, targetPort: WireMockServerArguments.HttpContainerPort)
            .WithHealthCheck(healthCheckKey)
            .WithWireMockInspectorCommand();

        // Add HTTPS endpoint if configured
        if (arguments.UseHttps)
        {
            resourceBuilder = resourceBuilder.WithHttpsEndpoint(
                port: arguments.HttpsPort,
                targetPort: WireMockServerArguments.HttpsContainerPort,
                name: "https");
        }

        if (!string.IsNullOrEmpty(arguments.MappingsPath))
        {
            resourceBuilder = resourceBuilder.WithBindMount(arguments.MappingsPath, DefaultLinuxMappingsPath);
        }

        resourceBuilder = resourceBuilder.WithArgs(ctx =>
        {
            foreach (var arg in arguments.GetArgs())
            {
                ctx.Args.Add(arg);
            }
        });

        return resourceBuilder;
    }

    /// <summary>
    /// Adds a WireMock.Net Server resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="callback">A callback that allows for setting the <see cref="WireMockServerArguments"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> AddWireMock(this IDistributedApplicationBuilder builder, string name, Action<WireMockServerArguments> callback)
    {
        Guard.NotNull(builder);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(callback);

        var arguments = new WireMockServerArguments();
        callback(arguments);

        return builder.AddWireMock(name, arguments);
    }

    /// <summary>
    /// Defines if the static mappings should be read at startup.
    ///
    /// Default set to <c>false</c>.
    /// </summary>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithReadStaticMappings(this IResourceBuilder<WireMockServerResource> wiremock)
    {
        Guard.NotNull(wiremock).Resource.Arguments.ReadStaticMappings = true;
        return wiremock;
    }

    /// <summary>
    /// Watch the static mapping files + folder for changes when running.
    ///
    /// Default set to <c>false</c>.
    /// </summary>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithWatchStaticMappings(this IResourceBuilder<WireMockServerResource> wiremock)
    {
        Guard.NotNull(wiremock).Resource.Arguments.WatchStaticMappings = true;
        return wiremock;
    }

    /// <summary>
    /// Specifies the path for the (static) mapping json files.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <param name="mappingsPath">The local path.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithMappingsPath(this IResourceBuilder<WireMockServerResource> wiremock, string mappingsPath)
    {
        Guard.NotNullOrWhiteSpace(mappingsPath);
        Guard.NotNull(wiremock).Resource.Arguments.MappingsPath = mappingsPath;

        return wiremock.WithBindMount(mappingsPath, DefaultLinuxMappingsPath);
    }

    /// <summary>
    /// Set the admin username and password for accessing the admin interface from WireMock.Net via HTTP.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <param name="username">The admin username.</param>
    /// <param name="password">The admin password.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    /// <remarks>
    /// <para><strong>Security Note:</strong> Credentials are passed as command-line arguments to the WireMock container
    /// and may be visible in container logs or process listings. For production scenarios, consider using
    /// secure credential management solutions like Azure Key Vault, environment variables from secure stores,
    /// or Aspire's secret management features.</para>
    /// <para>The credentials are transmitted as HTTP Basic Authentication headers (Base64 encoded) when
    /// making admin API calls. Ensure HTTPS is used in production environments to protect credentials in transit.</para>
    /// </remarks>
    public static IResourceBuilder<WireMockServerResource> WithAdminUserNameAndPassword(this IResourceBuilder<WireMockServerResource> wiremock, string username, string password)
    {
        Guard.NotNull(wiremock);

        wiremock.Resource.Arguments.AdminUsername = Guard.NotNull(username);
        wiremock.Resource.Arguments.AdminPassword = Guard.NotNull(password);
        return wiremock;
    }

    /// <summary>
    /// Use WireMock Client's AdminApiMappingBuilder to configure the WireMock.Net resource.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <param name="configure">Delegate that will be invoked to configure the WireMock.Net resource.</param>
    /// <returns></returns>
    public static IResourceBuilder<WireMockServerResource> WithApiMappingBuilder(this IResourceBuilder<WireMockServerResource> wiremock, Func<AdminApiMappingBuilder, Task> configure)
    {
        return wiremock.WithApiMappingBuilder((adminApiMappingBuilder, _) => configure.Invoke(adminApiMappingBuilder));
    }

    /// <summary>
    /// Use WireMock Client's AdminApiMappingBuilder to configure the WireMock.Net resource.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <param name="configure">Delegate that will be invoked to configure the WireMock.Net resource.</param>
    /// <returns></returns>
    public static IResourceBuilder<WireMockServerResource> WithApiMappingBuilder(this IResourceBuilder<WireMockServerResource> wiremock, Func<AdminApiMappingBuilder, CancellationToken, Task> configure)
    {
        Guard.NotNull(wiremock);

        wiremock.ApplicationBuilder.Services.TryAddLifecycleHook<WireMockServerLifecycleHook>();
        wiremock.Resource.Arguments.ApiMappingBuilder = configure;
        wiremock.Resource.ApiMappingState = WireMockMappingState.NotSubmitted;

        return wiremock;
    }

    /// <summary>
    /// Enables the WireMockInspect, a cross-platform UI app that facilitates WireMock troubleshooting.
    /// This requires installation of the WireMockInspector tool.
    /// <code>
    /// dotnet tool install WireMockInspector --global --no-cache --ignore-failed-sources
    /// </code>
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{WireMockNetResource}"/>.</param>
    /// <returns></returns>
    public static IResourceBuilder<WireMockServerResource> WithWireMockInspectorCommand(this IResourceBuilder<WireMockServerResource> builder)
    {
        Guard.NotNull(builder);

        CommandOptions commandOptions = new()
        {
            Description = "Requires installation of the WireMockInspector (https://github.com/WireMock-Net/WireMockInspector) tool:\ndotnet tool install WireMockInspector --global --no-cache --ignore-failed-sources",
            UpdateState = OnUpdateResourceState,
            IconName = "BoxSearch",
            IconVariant = IconVariant.Filled
        };

        builder.WithCommand(
            name: "wiremock-inspector",
            displayName: "WireMock Inspector",
            executeCommand: _ => OnRunOpenInspectorCommandAsync(builder),
            commandOptions: commandOptions);

        return builder;
    }

    #region HTTPS Configuration

    /// <summary>
    /// Adds an HTTPS endpoint to the WireMock.Net server using the container's default self-signed certificate.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <param name="port">The optional HTTPS port. If not specified, a random port is assigned.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithHttpsEndpoint(this IResourceBuilder<WireMockServerResource> wiremock, int? port = null)
    {
        Guard.NotNull(wiremock);
        Guard.Condition(port, p => p is null or > 0 and <= ushort.MaxValue);

        wiremock.Resource.Arguments.HttpsPort = port;
        return wiremock;
    }

    #endregion

    #region Request Logging Configuration

    /// <summary>
    /// Sets the maximum number of request log entries to retain.
    /// When the limit is reached, older entries are automatically removed.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <param name="count">The maximum number of log entries to retain.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithMaxRequestLogCount(this IResourceBuilder<WireMockServerResource> wiremock, int count)
    {
        Guard.NotNull(wiremock);
        Guard.Condition(count, c => c > 0);

        wiremock.Resource.Arguments.MaxRequestLogCount = count;
        return wiremock;
    }

    /// <summary>
    /// Sets the request log expiration duration in hours.
    /// Entries older than this duration are automatically removed.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <param name="hours">The expiration duration in hours.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithRequestLogExpiration(this IResourceBuilder<WireMockServerResource> wiremock, int hours)
    {
        Guard.NotNull(wiremock);
        Guard.Condition(hours, h => h > 0);

        wiremock.Resource.Arguments.RequestLogExpirationDuration = hours;
        return wiremock;
    }

    /// <summary>
    /// Save unmatched requests to a file for debugging purposes.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithSaveUnmatchedRequests(this IResourceBuilder<WireMockServerResource> wiremock)
    {
        Guard.NotNull(wiremock).Resource.Arguments.SaveUnmatchedRequests = true;
        return wiremock;
    }

    #endregion

    #region Request Processing Configuration

    /// <summary>
    /// Allow partial mapping matching. When enabled, requests can partially match mappings.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithAllowPartialMapping(this IResourceBuilder<WireMockServerResource> wiremock)
    {
        Guard.NotNull(wiremock).Resource.Arguments.AllowPartialMapping = true;
        return wiremock;
    }

    /// <summary>
    /// Allow request body for all HTTP methods (including GET, DELETE).
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithAllowBodyForAllHttpMethods(this IResourceBuilder<WireMockServerResource> wiremock)
    {
        Guard.NotNull(wiremock).Resource.Arguments.AllowBodyForAllHttpMethods = true;
        return wiremock;
    }

    /// <summary>
    /// Handle requests synchronously instead of asynchronously.
    /// Useful for debugging scenarios.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithSynchronousRequestHandling(this IResourceBuilder<WireMockServerResource> wiremock)
    {
        Guard.NotNull(wiremock).Resource.Arguments.HandleRequestsSynchronously = true;
        return wiremock;
    }

    #endregion

    #region Server Configuration

    /// <summary>
    /// Configure whether the admin interface is enabled.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <param name="enabled">Whether to enable the admin interface. Default is true.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithAdminInterface(this IResourceBuilder<WireMockServerResource> wiremock, bool enabled = true)
    {
        Guard.NotNull(wiremock).Resource.Arguments.StartAdminInterface = enabled;
        return wiremock;
    }

    /// <summary>
    /// Set a custom path for the admin interface.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <param name="path">The custom admin path. Default is "/__admin".</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithAdminPath(this IResourceBuilder<WireMockServerResource> wiremock, string path)
    {
        Guard.NotNull(wiremock);
        Guard.NotNullOrWhiteSpace(path);

        wiremock.Resource.Arguments.AdminPath = path;
        return wiremock;
    }

    #endregion

    #region Azure AD Authentication

    /// <summary>
    /// Set Azure AD authentication for the admin interface.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <param name="tenant">The Azure AD tenant.</param>
    /// <param name="audience">The Azure AD audience.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithAzureADAuthentication(this IResourceBuilder<WireMockServerResource> wiremock, string tenant, string audience)
    {
        Guard.NotNull(wiremock);
        Guard.NotNullOrWhiteSpace(tenant);
        Guard.NotNullOrWhiteSpace(audience);

        wiremock.Resource.Arguments.AdminAzureADTenant = tenant;
        wiremock.Resource.Arguments.AdminAzureADAudience = audience;
        return wiremock;
    }

    #endregion

    #region OpenAPI Configuration

    /// <summary>
    /// Load mappings from an OpenAPI specification file.
    /// Supports OpenAPI 2.0 (Swagger), 3.0, 3.1, and RAML formats in JSON or YAML.
    /// The file is read and sent to the WireMock server's OpenAPI endpoint at startup.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <param name="filePath">The local path to the OpenAPI specification file.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithOpenApiFile(this IResourceBuilder<WireMockServerResource> wiremock, string filePath)
    {
        Guard.NotNull(wiremock);
        Guard.NotNullOrWhiteSpace(filePath);

        // Validate file path to prevent path traversal attacks
        try
        {
            // Validate that the path is well-formed
            _ = Path.GetFullPath(filePath);

            // Check for common path traversal patterns
            if (filePath.Contains("..", StringComparison.Ordinal) && !Path.IsPathRooted(filePath))
            {
                throw new ArgumentException(
                    "File path contains potentially unsafe path traversal patterns (..). " +
                    "Use absolute paths or ensure relative paths don't traverse outside the working directory.",
                    nameof(filePath));
            }
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException($"Invalid file path: {filePath}", nameof(filePath), ex);
        }

        wiremock.Resource.Arguments.OpenApiFilePath = filePath;
        wiremock.ApplicationBuilder.Services.TryAddLifecycleHook<WireMockServerLifecycleHook>();

        return wiremock;
    }

    /// <summary>
    /// Load mappings from an OpenAPI specification string.
    /// Supports OpenAPI 2.0 (Swagger), 3.0, 3.1, and RAML formats in JSON or YAML.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <param name="openApiContent">The OpenAPI specification content.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithOpenApiDocument(this IResourceBuilder<WireMockServerResource> wiremock, string openApiContent)
    {
        Guard.NotNull(wiremock);
        Guard.NotNullOrWhiteSpace(openApiContent);

        wiremock.Resource.Arguments.OpenApiDocument = openApiContent;
        wiremock.ApplicationBuilder.Services.TryAddLifecycleHook<WireMockServerLifecycleHook>();

        return wiremock;
    }

    /// <summary>
    /// Load mappings from an OpenAPI specification using a factory function.
    /// Useful for loading from embedded resources or dynamic content.
    /// Supports OpenAPI 2.0 (Swagger), 3.0, 3.1, and RAML formats in JSON or YAML.
    /// </summary>
    /// <param name="wiremock">The <see cref="IResourceBuilder{WireMockServerResource}"/>.</param>
    /// <param name="contentFactory">A function that returns the OpenAPI specification content.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{WireMockServerResource}"/>.</returns>
    public static IResourceBuilder<WireMockServerResource> WithOpenApiDocument(this IResourceBuilder<WireMockServerResource> wiremock, Func<string> contentFactory)
    {
        Guard.NotNull(wiremock);
        Guard.NotNull(contentFactory);

        wiremock.Resource.Arguments.OpenApiDocumentFactory = contentFactory;
        wiremock.ApplicationBuilder.Services.TryAddLifecycleHook<WireMockServerLifecycleHook>();

        return wiremock;
    }

    #endregion

    private static Task<ExecuteCommandResult> OnRunOpenInspectorCommandAsync(IResourceBuilder<WireMockServerResource> builder)
    {
        WireMockInspector.Inspect(builder.Resource.GetEndpoint().Url);

        return Task.FromResult(CommandResults.Success());
    }

    private static ResourceCommandState OnUpdateResourceState(UpdateCommandStateContext context)
    {
        return context.ResourceSnapshot.HealthStatus is HealthStatus.Healthy
            ? ResourceCommandState.Enabled
            : ResourceCommandState.Disabled;
    }
}
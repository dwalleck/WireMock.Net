// Copyright © WireMock.Net

using System.Diagnostics.CodeAnalysis;
using WireMock.Client.Builders;

// ReSharper disable once CheckNamespace
namespace Aspire.Hosting;

/// <summary>
/// Represents the arguments required to configure and start a WireMock.Net Server.
/// </summary>
public class WireMockServerArguments
{
    internal const int HttpContainerPort = 80;
    internal const int HttpsContainerPort = 443;

    /// <summary>
    /// The default HTTP port where WireMock.Net is listening.
    /// </summary>
    public const int DefaultPort = 9091;

    private const string DefaultLogger = "WireMockConsoleLogger";

    #region Port Configuration

    /// <summary>
    /// The HTTP port where WireMock.Net is listening.
    /// If not defined, .NET Aspire automatically assigns a random port.
    /// </summary>
    public int? HttpPort { get; set; }

    /// <summary>
    /// The HTTPS port where WireMock.Net is listening.
    /// If not defined, HTTPS is not enabled.
    /// </summary>
    public int? HttpsPort { get; set; }

    /// <summary>
    /// Indicates whether HTTPS is enabled.
    /// </summary>
    public bool UseHttps => HttpsPort.HasValue;

    #endregion

    #region Basic Authentication

    /// <summary>
    /// The admin username.
    /// </summary>
    [MemberNotNullWhen(true, nameof(HasBasicAuthentication))]
    public string? AdminUsername { get; set; }

    /// <summary>
    /// The admin password.
    /// </summary>
    [MemberNotNullWhen(true, nameof(HasBasicAuthentication))]
    public string? AdminPassword { get; set; }

    /// <summary>
    /// Indicates whether the admin interface has Basic Authentication.
    /// </summary>
    public bool HasBasicAuthentication => !string.IsNullOrEmpty(AdminUsername) && !string.IsNullOrEmpty(AdminPassword);

    #endregion

    #region Azure AD Authentication

    /// <summary>
    /// The Azure AD tenant for admin authentication.
    /// </summary>
    [MemberNotNullWhen(true, nameof(HasAzureADAuthentication))]
    public string? AdminAzureADTenant { get; set; }

    /// <summary>
    /// The Azure AD audience for admin authentication.
    /// </summary>
    [MemberNotNullWhen(true, nameof(HasAzureADAuthentication))]
    public string? AdminAzureADAudience { get; set; }

    /// <summary>
    /// Indicates whether the admin interface has Azure AD Authentication.
    /// </summary>
    public bool HasAzureADAuthentication => !string.IsNullOrEmpty(AdminAzureADTenant) && !string.IsNullOrEmpty(AdminAzureADAudience);

    #endregion

    #region Static Mappings

    /// <summary>
    /// Defines if the static mappings should be read at startup.
    ///
    /// Default value is <c>false</c>.
    /// </summary>
    public bool ReadStaticMappings { get; set; }

    /// <summary>
    /// Watch the static mapping files + folder for changes when running.
    ///
    /// Default value is <c>false</c>.
    /// </summary>
    public bool WatchStaticMappings { get; set; }

    /// <summary>
    /// Specifies the path for the (static) mapping json files.
    /// </summary>
    public string? MappingsPath { get; set; }

    #endregion

    #region Request Logging

    /// <summary>
    /// The maximum number of request log entries to retain.
    /// When the limit is reached, older entries are automatically removed.
    /// </summary>
    public int? MaxRequestLogCount { get; set; }

    /// <summary>
    /// The request log expiration duration in hours.
    /// Entries older than this duration are automatically removed.
    /// </summary>
    public int? RequestLogExpirationDuration { get; set; }

    /// <summary>
    /// Save unmatched requests to a file for debugging purposes.
    /// Default value is <c>false</c>.
    /// </summary>
    public bool SaveUnmatchedRequests { get; set; }

    /// <summary>
    /// Do not save the dynamic response in the log entry.
    /// Default value is <c>false</c>.
    /// </summary>
    public bool DoNotSaveDynamicResponseInLogEntry { get; set; }

    #endregion

    #region Request Processing

    /// <summary>
    /// Allow partial mapping matching.
    /// Default value is <c>false</c>.
    /// </summary>
    public bool AllowPartialMapping { get; set; }

    /// <summary>
    /// Allow request body for all HTTP methods (including GET, DELETE).
    /// Default value is <c>false</c>.
    /// </summary>
    public bool AllowBodyForAllHttpMethods { get; set; }

    /// <summary>
    /// Disable JSON body parsing when processing requests.
    /// Default value is <c>false</c>.
    /// </summary>
    public bool DisableJsonBodyParsing { get; set; }

    /// <summary>
    /// Disable request body decompressing (gzip/deflate).
    /// Default value is <c>false</c>.
    /// </summary>
    public bool DisableRequestBodyDecompressing { get; set; }

    /// <summary>
    /// Handle requests synchronously instead of asynchronously.
    /// Default value is <c>false</c>.
    /// </summary>
    public bool HandleRequestsSynchronously { get; set; }

    /// <summary>
    /// Use extended regex syntax.
    /// Default value in WireMock is <c>true</c>.
    /// </summary>
    public bool? UseRegexExtended { get; set; }

    #endregion

    #region Server Configuration

    /// <summary>
    /// Whether to start the admin interface.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool? StartAdminInterface { get; set; }

    /// <summary>
    /// Custom path for the admin interface.
    /// Default value is "/__admin".
    /// </summary>
    public string? AdminPath { get; set; }

    /// <summary>
    /// Server startup timeout in milliseconds.
    /// Default value is 10000ms.
    /// </summary>
    public int? StartTimeout { get; set; }

    #endregion

    #region OpenAPI Configuration

    /// <summary>
    /// Path to an OpenAPI specification file to load as mappings.
    /// Supports OpenAPI 2.0 (Swagger), 3.0, 3.1, and RAML formats in JSON or YAML.
    /// </summary>
    public string? OpenApiFilePath { get; set; }

    /// <summary>
    /// URL to an OpenAPI specification to load as mappings.
    /// Supports OpenAPI 2.0 (Swagger), 3.0, 3.1, and RAML formats in JSON or YAML.
    /// </summary>
    public string? OpenApiUrl { get; set; }

    /// <summary>
    /// OpenAPI specification content to load as mappings.
    /// </summary>
    public string? OpenApiDocument { get; set; }

    /// <summary>
    /// Factory function to get OpenAPI specification content.
    /// Useful for loading from embedded resources or dynamic content.
    /// </summary>
    public Func<string>? OpenApiDocumentFactory { get; set; }

    /// <summary>
    /// Async factory function to get OpenAPI specification content.
    /// Useful for loading from embedded resources, databases, or network sources asynchronously.
    /// </summary>
    public Func<Task<string>>? OpenApiDocumentFactoryAsync { get; set; }

    /// <summary>
    /// Indicates whether OpenAPI configuration is present.
    /// </summary>
    public bool HasOpenApiConfiguration =>
        !string.IsNullOrEmpty(OpenApiFilePath) ||
        !string.IsNullOrEmpty(OpenApiUrl) ||
        !string.IsNullOrEmpty(OpenApiDocument) ||
        OpenApiDocumentFactory != null ||
        OpenApiDocumentFactoryAsync != null;

    /// <summary>
    /// If true, OpenAPI load failures will cause the application startup to fail.
    /// If false (default), failures are logged as errors but startup continues.
    /// </summary>
    public bool ThrowOnOpenApiLoadFailure { get; set; }

    #endregion

    /// <summary>
    /// Optional delegate that will be invoked to configure the WireMock.Net resource using the <see cref="AdminApiMappingBuilder"/>.
    /// </summary>
    public Func<AdminApiMappingBuilder, CancellationToken, Task>? ApiMappingBuilder { get; set; }

    /// <summary>
    /// Converts the current instance's properties to an array of command-line arguments for starting the WireMock.Net server.
    /// </summary>
    /// <returns>An array of strings representing the command-line arguments.</returns>
    public string[] GetArgs()
    {
        var args = new Dictionary<string, string>();

        Add(args, "--WireMockLogger", DefaultLogger);

        // Basic Authentication
        if (HasBasicAuthentication)
        {
            Add(args, "--AdminUserName", AdminUsername!);
            Add(args, "--AdminPassword", AdminPassword!);
        }

        // Azure AD Authentication
        if (HasAzureADAuthentication)
        {
            Add(args, "--AdminAzureADTenant", AdminAzureADTenant!);
            Add(args, "--AdminAzureADAudience", AdminAzureADAudience!);
        }

        // Static Mappings
        if (ReadStaticMappings)
        {
            Add(args, "--ReadStaticMappings", "true");
        }

        if (WatchStaticMappings)
        {
            Add(args, "--ReadStaticMappings", "true");
            Add(args, "--WatchStaticMappings", "true");
            Add(args, "--WatchStaticMappingsInSubdirectories", "true");
        }

        // Request Logging
        if (MaxRequestLogCount.HasValue)
        {
            Add(args, "--MaxRequestLogCount", MaxRequestLogCount.Value.ToString());
        }

        if (RequestLogExpirationDuration.HasValue)
        {
            Add(args, "--RequestLogExpirationDuration", RequestLogExpirationDuration.Value.ToString());
        }

        if (SaveUnmatchedRequests)
        {
            Add(args, "--SaveUnmatchedRequests", "true");
        }

        if (DoNotSaveDynamicResponseInLogEntry)
        {
            Add(args, "--DoNotSaveDynamicResponseInLogEntry", "true");
        }

        // Request Processing
        if (AllowPartialMapping)
        {
            Add(args, "--AllowPartialMapping", "true");
        }

        if (AllowBodyForAllHttpMethods)
        {
            Add(args, "--AllowBodyForAllHttpMethods", "true");
        }

        if (DisableJsonBodyParsing)
        {
            Add(args, "--DisableJsonBodyParsing", "true");
        }

        if (DisableRequestBodyDecompressing)
        {
            Add(args, "--DisableRequestBodyDecompressing", "true");
        }

        if (HandleRequestsSynchronously)
        {
            Add(args, "--HandleRequestsSynchronously", "true");
        }

        if (UseRegexExtended.HasValue)
        {
            Add(args, "--UseRegexExtended", UseRegexExtended.Value.ToString().ToLowerInvariant());
        }

        // Server Configuration
        if (StartAdminInterface.HasValue)
        {
            Add(args, "--StartAdminInterface", StartAdminInterface.Value.ToString().ToLowerInvariant());
        }

        if (!string.IsNullOrEmpty(AdminPath))
        {
            Add(args, "--AdminPath", AdminPath);
        }

        if (StartTimeout.HasValue)
        {
            Add(args, "--StartTimeout", StartTimeout.Value.ToString());
        }

        return args
            .SelectMany(k => new[] { k.Key, k.Value })
            .ToArray();
    }

    private static void Add(IDictionary<string, string> args, string argument, string value)
    {
        args[argument] = value;
    }
}
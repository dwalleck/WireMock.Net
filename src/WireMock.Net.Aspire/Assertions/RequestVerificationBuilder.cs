// Copyright © WireMock.Net

using System.Globalization;
using WireMock.Admin.Requests;

// ReSharper disable once CheckNamespace
namespace Aspire.Hosting;

/// <summary>
/// Fluent builder for request verification assertions.
/// </summary>
public class RequestVerificationBuilder
{
    private readonly WireMockAssertionContext _context;
    private string? _method;
    private string? _path;
    private readonly Dictionary<string, string> _headers = new();
    private string? _bodyContains;
    private int? _expectedCount;
    private bool _atLeastOnce = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestVerificationBuilder"/> class.
    /// </summary>
    /// <param name="context">The assertion context.</param>
    internal RequestVerificationBuilder(WireMockAssertionContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Filter requests by HTTP method.
    /// </summary>
    /// <param name="method">The HTTP method (GET, POST, PUT, DELETE, etc.).
    /// Use constants from <see cref="Microsoft.AspNetCore.Http.HttpMethods"/> (e.g., HttpMethods.Get)
    /// to avoid magic strings.</param>
    /// <returns>The builder for chaining.</returns>
    public RequestVerificationBuilder WithMethod(string method)
    {
        _method = method;
        return this;
    }

    /// <summary>
    /// Filter requests by path.
    /// </summary>
    /// <param name="path">The request path to match.</param>
    /// <returns>The builder for chaining.</returns>
    public RequestVerificationBuilder WithPath(string path)
    {
        _path = path;
        return this;
    }

    /// <summary>
    /// Filter requests by header.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The expected header value.</param>
    /// <returns>The builder for chaining.</returns>
    public RequestVerificationBuilder WithHeader(string name, string value)
    {
        _headers[name] = value;
        return this;
    }

    /// <summary>
    /// Filter requests by body content.
    /// </summary>
    /// <param name="content">The content that the body should contain.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// This method performs a case-insensitive string comparison on the request body.
    /// It works best with text-based content. For JSON bodies, ensure the content
    /// parameter matches the actual JSON structure as a string (e.g., including quotes
    /// and formatting). Binary content is not supported by this method.
    /// </remarks>
    public RequestVerificationBuilder WithBodyContaining(string content)
    {
        _bodyContains = content;
        return this;
    }

    /// <summary>
    /// Expect exactly the specified number of matching requests.
    /// </summary>
    /// <param name="count">The expected count.</param>
    /// <returns>The builder for chaining.</returns>
    public RequestVerificationBuilder Times(int count)
    {
        _expectedCount = count;
        _atLeastOnce = false;
        return this;
    }

    /// <summary>
    /// Expect at least one matching request. This is the default behavior.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public RequestVerificationBuilder AtLeastOnce()
    {
        _expectedCount = null;
        _atLeastOnce = true;
        return this;
    }

    /// <summary>
    /// Execute the verification and return the context for chaining.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The assertion context for chaining.</returns>
    /// <exception cref="WireMockAssertionException">Thrown when the assertion fails.</exception>
    public async Task<WireMockAssertionContext> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _context.AdminApi.GetRequestsAsync(cancellationToken);
        var matching = FilterRequests(requests);

        ValidateCount(matching.Count);
        return _context;
    }

    private List<LogEntryModel> FilterRequests(IEnumerable<LogEntryModel> requests)
    {
        // Combine all filters into a single predicate to reduce iterations
        return requests.Where(r =>
        {
            // Filter by method
            if (!string.IsNullOrEmpty(_method) &&
                r.Request?.Method?.Equals(_method, StringComparison.OrdinalIgnoreCase) != true)
            {
                return false;
            }

            // Filter by path
            if (!string.IsNullOrEmpty(_path) &&
                r.Request?.Path?.Equals(_path, StringComparison.OrdinalIgnoreCase) != true)
            {
                return false;
            }

            // Filter by headers
            if (_headers.Count > 0 && !MatchesHeaders(r))
            {
                return false;
            }

            // Filter by body content
            if (!string.IsNullOrEmpty(_bodyContains) &&
                r.Request?.Body?.Contains(_bodyContains, StringComparison.OrdinalIgnoreCase) != true)
            {
                return false;
            }

            return true;
        }).ToList();
    }

    private bool MatchesHeaders(LogEntryModel entry)
    {
        if (entry.Request?.Headers == null)
        {
            return false;
        }

        foreach (var header in _headers)
        {
            if (!entry.Request.Headers.TryGetValue(header.Key, out var values))
            {
                return false;
            }

            // Check if any value in the header list matches
            if (values == null || !values.Any(v => v.Contains(header.Value, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        return true;
    }

    private void ValidateCount(int actualCount)
    {
        var description = BuildDescription();

        if (_expectedCount.HasValue)
        {
            if (actualCount != _expectedCount.Value)
            {
                throw new WireMockAssertionException(
                    string.Format(CultureInfo.InvariantCulture,
                        "Expected exactly {0} request(s) matching {1}, but found {2}.",
                        _expectedCount.Value, description, actualCount));
            }
        }
        else if (_atLeastOnce && actualCount == 0)
        {
            throw new WireMockAssertionException(
                string.Format(CultureInfo.InvariantCulture,
                    "Expected at least one request matching {0}, but none were found.",
                    description));
        }
    }

    private string BuildDescription()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(_method))
        {
            parts.Add($"method={_method}");
        }

        if (!string.IsNullOrEmpty(_path))
        {
            parts.Add($"path={_path}");
        }

        if (_headers.Count > 0)
        {
            parts.Add($"headers=[{string.Join(",", _headers.Keys)}]");
        }

        if (!string.IsNullOrEmpty(_bodyContains))
        {
            parts.Add($"bodyContains=\"{_bodyContains}\"");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "any request";
    }
}

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
    private readonly Dictionary<string, string> _queryParams = new();
    private string? _bodyContains;
    private string? _bodyJson;
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
    /// Filter requests by query parameter.
    /// </summary>
    /// <param name="name">The query parameter name.</param>
    /// <param name="value">The expected query parameter value.</param>
    /// <returns>The builder for chaining.</returns>
    public RequestVerificationBuilder WithQueryParam(string name, string value)
    {
        _queryParams[name] = value;
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
    /// Filter requests by JSON body content with semantic equivalence.
    /// </summary>
    /// <param name="expectedJson">The expected JSON content as a string.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// This method performs a semantic JSON comparison, meaning that property order,
    /// whitespace, and formatting differences are ignored. Only the actual JSON structure
    /// and values are compared.
    /// </remarks>
    public RequestVerificationBuilder WithBodyMatchingJson(string expectedJson)
    {
        _bodyJson = expectedJson;
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

        // Count the matching requests without materializing to a list
        // This is more memory-efficient for large request logs
        ValidateCount(matching.Count());
        return _context;
    }

    private IEnumerable<LogEntryModel> FilterRequests(IEnumerable<LogEntryModel> requests)
    {
        // Combine all filters into a single predicate to reduce iterations
        // Return IEnumerable instead of List to avoid unnecessary memory allocation
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

            // Filter by query parameters
            if (_queryParams.Count > 0 && !MatchesQueryParams(r))
            {
                return false;
            }

            // Filter by body content
            if (!string.IsNullOrEmpty(_bodyContains) &&
                r.Request?.Body?.Contains(_bodyContains, StringComparison.OrdinalIgnoreCase) != true)
            {
                return false;
            }

            // Filter by JSON body
            if (!string.IsNullOrEmpty(_bodyJson) && !MatchesJsonBody(r))
            {
                return false;
            }

            return true;
        });
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

    private bool MatchesQueryParams(LogEntryModel entry)
    {
        if (entry.Request?.Query == null)
        {
            return false;
        }

        foreach (var queryParam in _queryParams)
        {
            if (!entry.Request.Query.TryGetValue(queryParam.Key, out var values))
            {
                return false;
            }

            // Check if any value in the query parameter list matches
            if (values == null || !values.Any(v => v.Equals(queryParam.Value, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        return true;
    }

    private bool MatchesJsonBody(LogEntryModel entry)
    {
        if (string.IsNullOrEmpty(entry.Request?.Body))
        {
            return false;
        }

        try
        {
            // Parse both JSON strings and compare semantically
            using var expectedDoc = System.Text.Json.JsonDocument.Parse(_bodyJson!);
            using var actualDoc = System.Text.Json.JsonDocument.Parse(entry.Request.Body);

            // Compare the JSON elements semantically
            return JsonElementsEqual(expectedDoc.RootElement, actualDoc.RootElement);
        }
        catch
        {
            // If parsing fails, fall back to string comparison
            return false;
        }
    }

    private static bool JsonElementsEqual(System.Text.Json.JsonElement element1, System.Text.Json.JsonElement element2)
    {
        if (element1.ValueKind != element2.ValueKind)
        {
            return false;
        }

        switch (element1.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Object:
                var props1 = element1.EnumerateObject().OrderBy(p => p.Name).ToList();
                var props2 = element2.EnumerateObject().OrderBy(p => p.Name).ToList();

                if (props1.Count != props2.Count)
                {
                    return false;
                }

                for (int i = 0; i < props1.Count; i++)
                {
                    if (props1[i].Name != props2[i].Name || !JsonElementsEqual(props1[i].Value, props2[i].Value))
                    {
                        return false;
                    }
                }
                return true;

            case System.Text.Json.JsonValueKind.Array:
                var array1 = element1.EnumerateArray().ToList();
                var array2 = element2.EnumerateArray().ToList();

                if (array1.Count != array2.Count)
                {
                    return false;
                }

                for (int i = 0; i < array1.Count; i++)
                {
                    if (!JsonElementsEqual(array1[i], array2[i]))
                    {
                        return false;
                    }
                }
                return true;

            case System.Text.Json.JsonValueKind.String:
                return element1.GetString() == element2.GetString();

            case System.Text.Json.JsonValueKind.Number:
                return element1.GetDecimal() == element2.GetDecimal();

            case System.Text.Json.JsonValueKind.True:
            case System.Text.Json.JsonValueKind.False:
                return element1.GetBoolean() == element2.GetBoolean();

            case System.Text.Json.JsonValueKind.Null:
                return true;

            default:
                return false;
        }
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

        if (_queryParams.Count > 0)
        {
            parts.Add($"queryParams=[{string.Join(",", _queryParams.Keys)}]");
        }

        if (!string.IsNullOrEmpty(_bodyContains))
        {
            parts.Add($"bodyContains=\"{_bodyContains}\"");
        }

        if (!string.IsNullOrEmpty(_bodyJson))
        {
            parts.Add("bodyMatchesJson");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "any request";
    }
}

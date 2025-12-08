// Copyright © WireMock.Net

// ReSharper disable once CheckNamespace
namespace Aspire.Hosting;

/// <summary>
/// Exception thrown when a WireMock assertion fails.
/// </summary>
public class WireMockAssertionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WireMockAssertionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public WireMockAssertionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WireMockAssertionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public WireMockAssertionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

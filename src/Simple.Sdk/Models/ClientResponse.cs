namespace Simple.Sdk.Models;

/// <summary>
/// Unified response returned by <see cref="Abstractions.IClientProvider{TClient}"/> operations.
/// </summary>
public class ClientResponse
{
    /// <summary>
    /// Whether the operation succeeded at both the transport and provider API levels.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// HTTP status code returned by the remote endpoint (0 if a transport error occurred).
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Provider-assigned message identifier (e.g. Slack <c>ts</c>), when available.
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// Channel or destination associated with the response.
    /// </summary>
    public string? Channel { get; init; }

    /// <summary>
    /// Raw response body returned by the remote endpoint.
    /// </summary>
    public string? RawBody { get; init; }

    /// <summary>
    /// Error message when <see cref="IsSuccess"/> is <see langword="false"/>.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Messages returned by a read operation.
    /// </summary>
    public IReadOnlyList<ClientMessage>? Messages { get; init; }

    /// <summary>
    /// Pagination cursor for fetching subsequent pages of messages.
    /// </summary>
    public string? NextCursor { get; init; }
}

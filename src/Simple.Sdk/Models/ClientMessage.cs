namespace Simple.Sdk.Models;

/// <summary>
/// Represents a single message returned from a read operation.
/// </summary>
public class ClientMessage
{
    /// <summary>
    /// Provider-assigned message identifier (e.g. Slack <c>ts</c>).
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Author or user identifier.
    /// </summary>
    public string? User { get; init; }

    /// <summary>
    /// Message text content.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Parent thread identifier, if the message belongs to a thread.
    /// </summary>
    public string? ThreadTs { get; init; }
}

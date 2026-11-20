namespace Simple.Sdk.Models;

/// <summary>
/// Parameters for reading messages via <see cref="Abstractions.IClientProvider{TClient}.ReadAsync"/>.
/// </summary>
public class ReadRequest
{
    /// <summary>
    /// Optional target channel or conversation ID. Falls back to provider options if omitted.
    /// </summary>
    public string? Channel { get; set; }

    /// <summary>
    /// Optional parent thread timestamp. When specified, reads replies in that thread.
    /// </summary>
    public string? ThreadTs { get; set; }

    /// <summary>
    /// Maximum number of messages to return. Defaults to 20.
    /// </summary>
    public int Limit { get; set; } = 20;

    /// <summary>
    /// Optional pagination cursor from a previous <see cref="ClientResponse.NextCursor"/>.
    /// </summary>
    public string? Cursor { get; set; }
}

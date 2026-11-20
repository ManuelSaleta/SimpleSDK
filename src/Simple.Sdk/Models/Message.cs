namespace Simple.Sdk.Models;

/// <summary>
/// Represents an outbound message to be posted via an <see cref="Abstractions.IClientProvider{TClient}"/>.
/// </summary>
public class Message
{
    public Message()
    {
    }

    public Message(string text, string? channel = null, string? threadTs = null)
    {
        Text = text;
        Channel = channel;
        ThreadTs = threadTs;
    }

    /// <summary>
    /// Primary message text (also used as fallback text when blocks are provided).
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Optional target channel or destination override. Falls back to provider options if omitted.
    /// </summary>
    public string? Channel { get; set; }

    /// <summary>
    /// Optional thread identifier (e.g. Slack <c>thread_ts</c>) to post as a reply.
    /// </summary>
    public string? ThreadTs { get; set; }

    /// <summary>
    /// Optional display username override.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Optional icon emoji override (e.g. <c>:clapper:</c>).
    /// </summary>
    public string? IconEmoji { get; set; }

    /// <summary>
    /// Optional rich layout blocks (e.g. Slack Block Kit objects).
    /// </summary>
    public IReadOnlyList<object>? Blocks { get; set; }

    /// <summary>
    /// Optional extensible metadata attached by the caller.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; set; }
}

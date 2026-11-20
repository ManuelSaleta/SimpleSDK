namespace Simple.Sdk.Slack;

/// <summary>
/// Configuration options for <see cref="SlackClient"/>.
/// </summary>
public class SlackClientOptions
{
    /// <summary>
    /// Configuration section name for binding via <c>IConfiguration</c>.
    /// </summary>
    public const string SectionName = "Slack";

    /// <summary>
    /// Slack Incoming Webhook URL for write-only notifications.
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Slack Bot User OAuth Token (<c>xoxb-...</c>) for Web API posting and reading.
    /// </summary>
    public string? BotToken { get; set; }

    /// <summary>
    /// Default Slack channel ID or name (e.g. <c>C01234567</c> or <c>#media</c>) used when a message omits <c>Channel</c>.
    /// </summary>
    public string? DefaultChannel { get; set; }

    /// <summary>
    /// Base URL for the Slack Web API. Defaults to <c>https://slack.com/api/</c>.
    /// </summary>
    public string BaseApiUrl { get; set; } = "https://slack.com/api/";

    /// <summary>
    /// HTTP request timeout. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// When <see langword="true"/>, transport exceptions are rethrown instead of being captured in <see cref="Models.ClientResponse.Error"/>.
    /// </summary>
    public bool ThrowOnError { get; set; }

    /// <summary>
    /// Returns <see langword="true"/> when either <see cref="WebhookUrl"/> or <see cref="BotToken"/> is configured.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(WebhookUrl) || !string.IsNullOrWhiteSpace(BotToken);
}

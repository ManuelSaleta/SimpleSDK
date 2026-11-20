using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Simple.Sdk.Abstractions;
using Simple.Sdk.Models;

namespace Simple.Sdk.Slack;

/// <summary>
/// Lightweight Slack client implementing <see cref="IClientProvider{TClient}"/> over Incoming Webhooks and the Slack Web API.
/// </summary>
public class SlackClient : IClientProvider<SlackClient>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _httpClient;
    private readonly SlackClientOptions _options;
    private readonly ILogger<SlackClient> _logger;

    public SlackClient(
        HttpClient httpClient,
        IOptions<SlackClientOptions> options,
        ILogger<SlackClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _logger = logger ?? NullLogger<SlackClient>.Instance;
    }

    /// <inheritdoc />
    public async Task<ClientResponse?> PostAsync(Message message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!_options.IsConfigured)
        {
            _logger.LogDebug("SlackClient is not configured (neither WebhookUrl nor BotToken is set); skipping PostAsync.");
            return null;
        }

        var channel = ResolveChannel(message.Channel);
        var useBotApi = !string.IsNullOrWhiteSpace(_options.BotToken) &&
                        (!string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(_options.WebhookUrl));

        if (useBotApi)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                return new ClientResponse
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Error = "A Slack channel is required when posting with BotToken."
                };
            }

            return await PostViaBotApiAsync(message, channel, cancellationToken).ConfigureAwait(false);
        }

        return await PostViaWebhookAsync(message, channel, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ClientResponse?> ReadAsync(ReadRequest? request = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            _logger.LogDebug("SlackClient BotToken is not configured; skipping ReadAsync.");
            return null;
        }

        var channel = ResolveChannel(request?.Channel);
        if (string.IsNullOrWhiteSpace(channel))
        {
            _logger.LogWarning("No Slack channel specified and DefaultChannel is empty; skipping ReadAsync.");
            return null;
        }

        var limit = request?.Limit is > 0 ? request.Limit : 20;
        var isThread = !string.IsNullOrWhiteSpace(request?.ThreadTs);
        var endpoint = isThread ? "conversations.replies" : "conversations.history";

        var query = $"channel={Uri.EscapeDataString(channel)}&limit={limit}";
        if (isThread)
        {
            query += $"&ts={Uri.EscapeDataString(request!.ThreadTs!)}";
        }
        if (!string.IsNullOrWhiteSpace(request?.Cursor))
        {
            query += $"&cursor={Uri.EscapeDataString(request.Cursor)}";
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, BuildApiUri($"{endpoint}?{query}"));
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.BotToken);

        return await SendAndParseApiAsync(httpRequest, channel, parseMessages: true, cancellationToken).ConfigureAwait(false);
    }

    private Task<ClientResponse> PostViaBotApiAsync(
        Message message,
        string channel,
        CancellationToken cancellationToken)
    {
        var payload = new SlackPostPayload
        {
            Channel = channel,
            Text = message.Text,
            ThreadTs = message.ThreadTs,
            Username = message.Username,
            IconEmoji = message.IconEmoji,
            Blocks = message.Blocks
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildApiUri("chat.postMessage"))
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.BotToken);

        return SendAndParseApiAsync(httpRequest, channel, parseMessages: false, cancellationToken);
    }

    private async Task<ClientResponse> PostViaWebhookAsync(
        Message message,
        string? channel,
        CancellationToken cancellationToken)
    {
        var payload = new SlackPostPayload
        {
            Channel = channel,
            Text = message.Text,
            ThreadTs = message.ThreadTs,
            Username = message.Username,
            IconEmoji = message.IconEmoji,
            Blocks = message.Blocks
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.WebhookUrl!, UriKind.Absolute))
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var rawBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var statusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Slack webhook returned HTTP {StatusCode}: {Body}", statusCode, rawBody);
                return new ClientResponse
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    Channel = channel,
                    RawBody = rawBody,
                    Error = string.IsNullOrWhiteSpace(rawBody) ? response.ReasonPhrase : rawBody
                };
            }

            return new ClientResponse
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Channel = channel,
                RawBody = rawBody
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Slack webhook request failed.");
            if (_options.ThrowOnError) throw;

            return new ClientResponse
            {
                IsSuccess = false,
                StatusCode = 0,
                Channel = channel,
                Error = ex.Message
            };
        }
    }

    private async Task<ClientResponse> SendAndParseApiAsync(
        HttpRequestMessage httpRequest,
        string channel,
        bool parseMessages,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var rawBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var statusCode = (int)response.StatusCode;

            SlackApiResponse? parsed = null;
            if (!string.IsNullOrWhiteSpace(rawBody))
            {
                try
                {
                    parsed = JsonSerializer.Deserialize<SlackApiResponse>(rawBody, JsonOptions);
                }
                catch (JsonException)
                {
                    // Non-JSON response body (e.g. proxy error page); handled below via RawBody.
                }
            }

            var isSuccess = response.IsSuccessStatusCode && (parsed?.Ok ?? false);
            var error = isSuccess
                ? null
                : parsed?.Error ?? (string.IsNullOrWhiteSpace(rawBody) ? response.ReasonPhrase : rawBody);

            if (!isSuccess)
            {
                _logger.LogWarning("Slack Web API returned HTTP {StatusCode} (error={Error}).", statusCode, error);
            }

            IReadOnlyList<ClientMessage>? messages = null;
            if (parseMessages && parsed?.Messages is { Count: > 0 })
            {
                messages = parsed.Messages
                    .Select(m => new ClientMessage
                    {
                        Id = m.Ts,
                        User = m.User,
                        Text = m.Text ?? string.Empty,
                        ThreadTs = m.ThreadTs
                    })
                    .ToList()
                    .AsReadOnly();
            }

            return new ClientResponse
            {
                IsSuccess = isSuccess,
                StatusCode = statusCode,
                MessageId = parsed?.Ts,
                Channel = parsed?.Channel ?? channel,
                RawBody = rawBody,
                Error = error,
                Messages = messages,
                NextCursor = string.IsNullOrWhiteSpace(parsed?.ResponseMetadata?.NextCursor)
                    ? null
                    : parsed.ResponseMetadata.NextCursor
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Slack Web API request failed.");
            if (_options.ThrowOnError) throw;

            return new ClientResponse
            {
                IsSuccess = false,
                StatusCode = 0,
                Channel = channel,
                Error = ex.Message
            };
        }
    }

    private string? ResolveChannel(string? overrideChannel) =>
        !string.IsNullOrWhiteSpace(overrideChannel) ? overrideChannel.Trim() : _options.DefaultChannel?.Trim();

    private Uri BuildApiUri(string relativePath)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseApiUrl)
            ? "https://slack.com/api/"
            : _options.BaseApiUrl.TrimEnd('/') + "/";
        return new Uri(new Uri(baseUrl, UriKind.Absolute), relativePath);
    }

    private sealed class SlackPostPayload
    {
        public string? Channel { get; init; }
        public string Text { get; init; } = string.Empty;
        public string? ThreadTs { get; init; }
        public string? Username { get; init; }
        public string? IconEmoji { get; init; }
        public IReadOnlyList<object>? Blocks { get; init; }
    }

    private sealed class SlackApiResponse
    {
        public bool Ok { get; init; }
        public string? Error { get; init; }
        public string? Channel { get; init; }
        public string? Ts { get; init; }
        public List<SlackApiMessage>? Messages { get; init; }
        public SlackResponseMetadata? ResponseMetadata { get; init; }
    }

    private sealed class SlackApiMessage
    {
        public string? Ts { get; init; }
        public string? User { get; init; }
        public string? Text { get; init; }
        public string? ThreadTs { get; init; }
    }

    private sealed class SlackResponseMetadata
    {
        public string? NextCursor { get; init; }
    }
}

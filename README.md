# Simple.Sdk

Lightweight, DRY generic client provider SDK for .NET 10, built for homelab and automation services (`sync-sharp`, `jarvis-sharp`, etc.).

## Features

- **`IClientProvider<TClient>`** generic abstraction exposing:
  - `Task<ClientResponse?> PostAsync(Message message, CancellationToken cancellationToken = default)`
  - `Task<ClientResponse?> ReadAsync(ReadRequest? request = null, CancellationToken cancellationToken = default)`
- **`SlackClient`** supporting both **Incoming Webhooks** and **Bot Token (`chat.postMessage`, `conversations.history`, `conversations.replies`)**.
- **Graceful no-op** when unconfigured (returns `null` instead of throwing).
- **Zero third-party Slack SDK bloat** — built directly on `HttpClient` + `System.Text.Json`.
- **DI Helper**: `services.AddSlackClient(...)` via delegate, `SlackClientOptions`, or `IConfiguration`.

## Quick Start

### 1. Register via Dependency Injection

```csharp
using Simple.Sdk.Abstractions;
using Simple.Sdk.Extensions;
using Simple.Sdk.Models;
using Simple.Sdk.Slack;

builder.Services.AddSlackClient(opts =>
{
    opts.WebhookUrl = Environment.GetEnvironmentVariable("SLACK_WEBHOOK_URL");
    opts.BotToken = Environment.GetEnvironmentVariable("SLACK_BOT_TOKEN");
    opts.DefaultChannel = Environment.GetEnvironmentVariable("SLACK_NOTIFICATION_CHANNEL");
});
```

### 2. Inject and Use `IClientProvider<SlackClient>`

```csharp
public class MovieNotificationService(IClientProvider<SlackClient> slack)
{
    public async Task NotifyCompletedAsync(string title, int year, CancellationToken ct)
    {
        var response = await slack.PostAsync(new Message($"🎬 Downloaded *{title}* ({year})"), ct);
        // response is null when Slack is unconfigured (graceful no-op)
    }

    public async Task<ClientResponse?> GetRecentMessagesAsync(CancellationToken ct)
    {
        return await slack.ReadAsync(new ReadRequest { Limit = 10 }, ct);
    }
}
```

## License

MIT

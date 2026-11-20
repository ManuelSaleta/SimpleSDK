using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Simple.Sdk.Abstractions;
using Simple.Sdk.Models;
using Simple.Sdk.Slack;

namespace Simple.Sdk.Tests;

public class SlackClientTests
{
    [Fact]
    public async Task PostAsync_WhenUnconfigured_ReturnsNullGracefully()
    {
        var handler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("Should not be called"));
        var client = CreateClient(handler, new SlackClientOptions());

        var result = await client.PostAsync(new Message("Hello"));

        result.Should().BeNull();
        handler.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task PostAsync_ViaWebhook_SendsExpectedJsonAndReturnsSuccess()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ok", Encoding.UTF8, "text/plain")
        });

        var client = CreateClient(handler, new SlackClientOptions
        {
            WebhookUrl = "https://hooks.slack.com/services/T000/B000/XXXX",
            DefaultChannel = "#media"
        });

        var response = await client.PostAsync(new Message("Movie downloaded: Dune (2021)")
        {
            Username = "sync-sharp",
            IconEmoji = ":clapper:"
        });

        response.Should().NotBeNull();
        response!.IsSuccess.Should().BeTrue();
        response.StatusCode.Should().Be(200);
        response.Channel.Should().Be("#media");
        response.RawBody.Should().Be("ok");

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().Be("https://hooks.slack.com/services/T000/B000/XXXX");

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        doc.RootElement.GetProperty("text").GetString().Should().Be("Movie downloaded: Dune (2021)");
        doc.RootElement.GetProperty("channel").GetString().Should().Be("#media");
        doc.RootElement.GetProperty("username").GetString().Should().Be("sync-sharp");
        doc.RootElement.GetProperty("icon_emoji").GetString().Should().Be(":clapper:");
    }

    [Fact]
    public async Task PostAsync_ViaBotToken_CallsChatPostMessageAndParsesTs()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"ok":true,"channel":"C12345","ts":"1700000000.000100"}""",
                Encoding.UTF8,
                "application/json")
        });

        var client = CreateClient(handler, new SlackClientOptions
        {
            BotToken = "xoxb-test-token",
            DefaultChannel = "C12345"
        });

        var response = await client.PostAsync(new Message("Test alert") { ThreadTs = "1699999999.000050" });

        response.Should().NotBeNull();
        response!.IsSuccess.Should().BeTrue();
        response.MessageId.Should().Be("1700000000.000100");
        response.Channel.Should().Be("C12345");

        handler.LastRequest!.RequestUri!.ToString().Should().Be("https://slack.com/api/chat.postMessage");
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be("xoxb-test-token");

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        doc.RootElement.GetProperty("thread_ts").GetString().Should().Be("1699999999.000050");
    }

    [Fact]
    public async Task PostAsync_WhenSlackApiReturnsOkFalse_ReturnsFailureWithoutThrowing()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"ok":false,"error":"channel_not_found"}""", Encoding.UTF8, "application/json")
        });

        var client = CreateClient(handler, new SlackClientOptions
        {
            BotToken = "xoxb-test-token",
            DefaultChannel = "C_INVALID"
        });

        var response = await client.PostAsync(new Message("Hello"));

        response.Should().NotBeNull();
        response!.IsSuccess.Should().BeFalse();
        response.Error.Should().Be("channel_not_found");
    }

    [Fact]
    public async Task PostAsync_WhenHttpRequestThrowsAndThrowOnErrorFalse_ReturnsFailureResponse()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Connection refused"));
        var client = CreateClient(handler, new SlackClientOptions
        {
            WebhookUrl = "https://hooks.slack.com/services/T000/B000/XXXX",
            ThrowOnError = false
        });

        var response = await client.PostAsync(new Message("Hello"));

        response.Should().NotBeNull();
        response!.IsSuccess.Should().BeFalse();
        response.StatusCode.Should().Be(0);
        response.Error.Should().Contain("Connection refused");
    }

    [Fact]
    public async Task PostAsync_WhenHttpRequestThrowsAndThrowOnErrorTrue_Rethrows()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Connection refused"));
        var client = CreateClient(handler, new SlackClientOptions
        {
            WebhookUrl = "https://hooks.slack.com/services/T000/B000/XXXX",
            ThrowOnError = true
        });

        var act = () => client.PostAsync(new Message("Hello"));
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task ReadAsync_WhenBotTokenMissing_ReturnsNullGracefully()
    {
        var handler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("Should not be called"));
        var client = CreateClient(handler, new SlackClientOptions
        {
            WebhookUrl = "https://hooks.slack.com/services/T000/B000/XXXX"
        });

        var result = await client.ReadAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_ChannelHistory_ParsesMessagesAndCursor()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "ok": true,
                  "messages": [
                    { "ts": "1700000002.000200", "user": "U111", "text": "Second" },
                    { "ts": "1700000001.000100", "user": "U222", "text": "First", "thread_ts": "1700000000.000050" }
                  ],
                  "response_metadata": { "next_cursor": "cursor_abc" }
                }
                """,
                Encoding.UTF8,
                "application/json")
        });

        var client = CreateClient(handler, new SlackClientOptions
        {
            BotToken = "xoxb-test-token",
            DefaultChannel = "C12345"
        });

        var response = await client.ReadAsync(new ReadRequest { Limit = 10 });

        response.Should().NotBeNull();
        response!.IsSuccess.Should().BeTrue();
        response.NextCursor.Should().Be("cursor_abc");
        response.Messages.Should().HaveCount(2);
        response.Messages![0].Id.Should().Be("1700000002.000200");
        response.Messages[0].User.Should().Be("U111");
        response.Messages[0].Text.Should().Be("Second");
        response.Messages[1].ThreadTs.Should().Be("1700000000.000050");

        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().Be("https://slack.com/api/conversations.history?channel=C12345&limit=10");
    }

    [Fact]
    public async Task ReadAsync_WithThreadTs_CallsConversationsReplies()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"ok":true,"messages":[]}""", Encoding.UTF8, "application/json")
        });

        var client = CreateClient(handler, new SlackClientOptions
        {
            BotToken = "xoxb-test-token",
            DefaultChannel = "C12345"
        });

        var response = await client.ReadAsync(new ReadRequest { ThreadTs = "1700000000.000050", Limit = 5 });

        response.Should().NotBeNull();
        response!.IsSuccess.Should().BeTrue();
        handler.LastRequest!.RequestUri!.ToString().Should()
            .Be("https://slack.com/api/conversations.replies?channel=C12345&limit=5&ts=1700000000.000050");
    }

    private static IClientProvider<SlackClient> CreateClient(MockHttpMessageHandler handler, SlackClientOptions options)
    {
        var httpClient = new HttpClient(handler);
        return new SlackClient(httpClient, Options.Create(options));
    }
}

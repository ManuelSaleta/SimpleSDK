using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simple.Sdk.Abstractions;
using Simple.Sdk.Extensions;
using Simple.Sdk.Slack;

namespace Simple.Sdk.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSlackClient_WithDelegate_ResolvesIClientProviderOfSlackClient()
    {
        var services = new ServiceCollection();
        services.AddSlackClient(opts =>
        {
            opts.WebhookUrl = "https://hooks.slack.com/services/T000/B000/XXXX";
            opts.DefaultChannel = "#homelab";
        });

        using var provider = services.BuildServiceProvider();
        var clientProvider = provider.GetService<IClientProvider<SlackClient>>();
        var options = provider.GetRequiredService<IOptions<SlackClientOptions>>().Value;

        clientProvider.Should().NotBeNull();
        clientProvider.Should().BeOfType<SlackClient>();
        options.DefaultChannel.Should().Be("#homelab");
        options.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void AddSlackClient_WithOptionsInstance_BindsValues()
    {
        var services = new ServiceCollection();
        services.AddSlackClient(new SlackClientOptions
        {
            BotToken = "xoxb-123",
            DefaultChannel = "C999"
        });

        using var provider = services.BuildServiceProvider();
        var clientProvider = provider.GetService<IClientProvider<SlackClient>>();
        var options = provider.GetRequiredService<IOptions<SlackClientOptions>>().Value;

        clientProvider.Should().NotBeNull();
        options.BotToken.Should().Be("xoxb-123");
        options.DefaultChannel.Should().Be("C999");
    }

    [Fact]
    public void AddSlackClient_WithConfiguration_BindsSection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Slack:BotToken"] = "xoxb-from-config",
                ["Slack:DefaultChannel"] = "C777"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSlackClient(config);

        using var provider = services.BuildServiceProvider();
        var clientProvider = provider.GetService<IClientProvider<SlackClient>>();
        var options = provider.GetRequiredService<IOptions<SlackClientOptions>>().Value;

        clientProvider.Should().NotBeNull();
        options.BotToken.Should().Be("xoxb-from-config");
        options.DefaultChannel.Should().Be("C777");
    }
}

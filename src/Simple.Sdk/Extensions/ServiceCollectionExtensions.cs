using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simple.Sdk.Abstractions;
using Simple.Sdk.Slack;

namespace Simple.Sdk.Extensions;

/// <summary>
/// Dependency injection extension methods for registering <see cref="IClientProvider{TClient}"/> implementations.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SlackClient"/> and <see cref="IClientProvider{SlackClient}"/> configured via delegate.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Delegate to configure <see cref="SlackClientOptions"/>.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for further HTTP pipeline configuration.</returns>
    public static IHttpClientBuilder AddSlackClient(
        this IServiceCollection services,
        Action<SlackClientOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        return RegisterSlackHttpClient(services);
    }

    /// <summary>
    /// Registers <see cref="SlackClient"/> and <see cref="IClientProvider{SlackClient}"/> with pre-instantiated options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The options instance.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for further HTTP pipeline configuration.</returns>
    public static IHttpClientBuilder AddSlackClient(
        this IServiceCollection services,
        SlackClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return services.AddSlackClient(opt =>
        {
            opt.WebhookUrl = options.WebhookUrl;
            opt.BotToken = options.BotToken;
            opt.DefaultChannel = options.DefaultChannel;
            opt.BaseApiUrl = options.BaseApiUrl;
            opt.Timeout = options.Timeout;
            opt.ThrowOnError = options.ThrowOnError;
        });
    }

    /// <summary>
    /// Registers <see cref="SlackClient"/> and <see cref="IClientProvider{SlackClient}"/> bound from <see cref="IConfiguration"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration root or section.</param>
    /// <param name="sectionName">Section name to bind. Defaults to <see cref="SlackClientOptions.SectionName"/>.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for further HTTP pipeline configuration.</returns>
    public static IHttpClientBuilder AddSlackClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = SlackClientOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SlackClientOptions>(configuration.GetSection(sectionName));
        return RegisterSlackHttpClient(services);
    }

    private static IHttpClientBuilder RegisterSlackHttpClient(IServiceCollection services)
    {
        var builder = services.AddHttpClient<SlackClient>((sp, httpClient) =>
        {
            var opts = sp.GetRequiredService<IOptions<SlackClientOptions>>().Value;
            if (opts.Timeout > TimeSpan.Zero)
            {
                httpClient.Timeout = opts.Timeout;
            }
        });

        services.AddTransient<IClientProvider<SlackClient>>(sp => sp.GetRequiredService<SlackClient>());
        return builder;
    }
}

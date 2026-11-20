using Simple.Sdk.Models;

namespace Simple.Sdk.Abstractions;

/// <summary>
/// Generic DRY client provider abstraction for posting and reading messages across messaging/notification backends.
/// </summary>
/// <typeparam name="TClient">The concrete client type (e.g. <see cref="Slack.SlackClient"/>).</typeparam>
public interface IClientProvider<TClient> where TClient : class
{
    /// <summary>
    /// Posts a message to the configured destination.
    /// Returns <see langword="null"/> when the client is not configured or disabled.
    /// </summary>
    /// <param name="message">The message payload to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ClientResponse"/> describing the result, or <see langword="null"/> if unconfigured.</returns>
    Task<ClientResponse?> PostAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads messages from the configured destination.
    /// Returns <see langword="null"/> when the client is not configured for reading.
    /// </summary>
    /// <param name="request">Optional read parameters (channel, thread, limit, cursor).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ClientResponse"/> containing retrieved messages, or <see langword="null"/> if unconfigured.</returns>
    Task<ClientResponse?> ReadAsync(ReadRequest? request = null, CancellationToken cancellationToken = default);
}

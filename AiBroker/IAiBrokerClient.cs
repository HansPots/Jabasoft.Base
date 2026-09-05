namespace Jabasoft.Base.AiBroker;

/// <summary>
/// What every JabaSoft app uses to talk to Jabasoft.Broker instead of
/// calling Ollama/LM Studio directly. Same shape as each app's old
/// IChatClient/IEmbeddingClient/IAiConnectionTester/IAiModelCatalog
/// combined into one interface, since they're now all just HTTP calls to
/// the same broker process rather than four separate provider-aware
/// clients duplicated per app.
/// </summary>
public interface IAiBrokerClient
{
    Task<ChatResult> ChatAsync(ChatRequest request, CancellationToken cancellationToken);

    Task<EmbedResult> EmbedAsync(EmbedRequest request, CancellationToken cancellationToken);

    Task<ConnectionTestResult> TestConnectionAsync(AiProvider provider, string serverUrl, string model, CancellationToken cancellationToken);

    /// <summary>
    /// Also doubles as the "is it reachable" check - the broker caches this
    /// per (provider, serverUrl) for a short TTL, so a successful call here
    /// is proof of reachability without a separate round trip.
    /// </summary>
    Task<ModelListResult> ListModelsAsync(AiProvider provider, string serverUrl, CancellationToken cancellationToken);
}

namespace Jabasoft.Base.AiBroker;

/// <summary>
/// What every JabaSoft app uses to talk to Jabasoft.Broker instead of
/// calling Ollama/LM Studio directly - one HTTP-based contract shared by
/// every app, instead of each app carrying its own provider-aware client.
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

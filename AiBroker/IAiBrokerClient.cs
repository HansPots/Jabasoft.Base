namespace Jabasoft.Base.AiBroker;

/// <summary>
/// What every JabaSoft app uses to talk to Jabasoft.Broker instead of
/// calling Ollama/LM Studio directly - one HTTP-based contract shared by
/// every app, instead of each app carrying its own provider-aware client.
/// </summary>
public interface IAiBrokerClient
{
    Task<ChatResult> ChatAsync(ChatRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Hetzelfde gesprek, maar het antwoord komt stukje bij beetje binnen
    /// in plaats van in één keer aan het eind. Daarmee kan een scherm de
    /// tekst laten aangroeien terwijl het model typt, en een teller laten
    /// meelopen in plaats van twaalf seconden stil te staan.
    ///
    /// Het laatste stukje draagt <see cref="ChatStreamChunk.Done"/> en de
    /// tokens; ging er iets mis, dan draagt het de reden. De broker
    /// schrijft het verbruik ook in dat geval zelf weg - de aanroeper
    /// hoeft daar niets voor te doen.
    /// </summary>
    IAsyncEnumerable<ChatStreamChunk> ChatStreamAsync(ChatRequest request, CancellationToken cancellationToken);

    Task<EmbedResult> EmbedAsync(EmbedRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// (Her)indexeert de meegegeven bestanden van een project voor
    /// semantisch zoeken: elk bestand wordt geëmbed en weggeschreven in de
    /// AI-database. Opnieuw indexeren van hetzelfde bestand vervangt de
    /// oude rij.
    /// </summary>
    Task<SearchIndexResult> IndexSearchAsync(SearchIndexRequest request, CancellationToken cancellationToken);

    /// <summary>Zoekt semantisch binnen wat er voor dit project geïndexeerd staat - zie <see cref="IndexSearchAsync"/>.</summary>
    Task<SemanticSearchResult> SearchSemanticAsync(SemanticSearchRequest request, CancellationToken cancellationToken);

    Task<ConnectionTestResult> TestConnectionAsync(AiProvider provider, string serverUrl, string model, CancellationToken cancellationToken);

    /// <summary>
    /// Also doubles as the "is it reachable" check - the broker caches this
    /// per (provider, serverUrl) for a short TTL, so a successful call here
    /// is proof of reachability without a separate round trip.
    /// </summary>
    Task<ModelListResult> ListModelsAsync(AiProvider provider, string serverUrl, CancellationToken cancellationToken);

    /// <summary>
    /// De AI-instelling zoals die bij de broker staat: welke soort server,
    /// waar, en welke twee modellen. Er is er precies een voor de hele
    /// familie - een app leest hem dus, hij verzint hem niet zelf.
    /// </summary>
    Task<AiSettingsResult> GetSettingsAsync(CancellationToken cancellationToken);

    /// <summary>Zet een nieuwe instelling bij de broker neer. Geldt meteen voor elke applicatie die hem daarna opvraagt.</summary>
    Task<AiSettingsResult> SaveSettingsAsync(AiSettings settings, CancellationToken cancellationToken);

    /// <summary>
    /// De modellen die AANWEZIG zijn op de server die nu ingesteld staat.
    /// De broker kijkt zelf welke soort en welk adres dat is; de aanroeper
    /// hoeft dat dus niet te weten, en kan deze lijst rechtstreeks naast de
    /// ingestelde modelnamen leggen.
    /// </summary>
    Task<ModelListResult> ListConfiguredModelsAsync(CancellationToken cancellationToken);

    /// <summary>Of de tokentabel in JabasoftBase bereikbaar is - de gezondheidscontrole bij het opstarten.</summary>
    Task<TokenUsageStatus> GetUsageStatusAsync(CancellationToken cancellationToken);

    /// <summary>De weektotalen van het tokenverbruik, nieuwste eerst.</summary>
    Task<IReadOnlyList<TokenUsageWeek>> GetUsageWeeksAsync(CancellationToken cancellationToken);

    /// <summary>Het tokentotaal per model, het zwaarste eerst.</summary>
    Task<IReadOnlyList<TokenUsageModel>> GetUsageModelsAsync(CancellationToken cancellationToken);

    /// <summary>De losse aanroepen van één week, nieuwste eerst. <paramref name="week"/> is de sleutel uit <see cref="TokenUsageWeek.Week"/>.</summary>
    Task<IReadOnlyList<TokenUsageEntry>> GetUsageEntriesAsync(string week, CancellationToken cancellationToken);
}

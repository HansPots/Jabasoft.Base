using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;

namespace Jabasoft.Base.AiBroker;

/// <summary>
/// HTTP-based <see cref="IAiBrokerClient"/>. Register with
/// <c>services.AddHttpClient&lt;IAiBrokerClient, AiBrokerClient&gt;(c =&gt; { c.BaseAddress = new Uri(AiBrokerClient.DefaultBaseUrl); c.Timeout = TimeSpan.FromMinutes(5); })</c>
/// - the 5-minute timeout matters here, it must match the broker's own
/// chat-call timeout; the .NET default of 100s is too short for a slow
/// local model. Every failure mode (broker not running, request timeout,
/// bad JSON) is caught and turned into a failed result rather than an
/// exception.
/// </summary>
public sealed class AiBrokerClient(HttpClient httpClient) : IAiBrokerClient
{
    public const string DefaultBaseUrl = "http://localhost:5310";

    /// <summary>
    /// De broker schrijft AiProvider als tekst ("LmStudio"), niet als
    /// getal - zie ConfigureHttpJsonOptions in zijn Program.cs. Zonder deze
    /// converter loopt het teruglezen van een instelling daarop stuk.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Een client voor de broker op zijn standaardadres, voor een app die
    /// geen dependency injection heeft opgetuigd. De ruime wachttijd is
    /// dezelfde als hierboven beschreven: een traag lokaal model mag er
    /// minuten over doen.
    /// </summary>
    public static AiBrokerClient CreateDefault(string baseUrl = DefaultBaseUrl) =>
        new(new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(5) });

    public async Task<ChatResult> ChatAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new ChatResult(false, string.Empty, $"AI broker returned {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<ChatResult>(cancellationToken);
            return result ?? new ChatResult(false, string.Empty, "Empty response from the AI broker.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ChatResult(false, string.Empty, $"Could not reach the AI broker: {ex.Message}");
        }
    }

    /// <summary>
    /// Leest de stroom die /api/chat/stream teruggeeft: één JSON-object
    /// per regel (NDJSON). Geen SSE - beide kanten zijn van ons, en een
    /// regel lezen is minder werk dan een gebeurtenissenformaat uit
    /// elkaar halen.
    ///
    /// Belangrijk is ResponseHeadersRead: zonder dat wacht HttpClient tot
    /// het hele antwoord binnen is, en dan valt er niets meer te stromen.
    ///
    /// Wat opvalt in de vorm: een mislukking wordt eerst in een variabele
    /// gezet en pas daarna teruggegeven. Dat moet - C# staat geen yield
    /// in een catch toe - maar het leest ook eerlijker: er is één plek
    /// waar het einde vandaan komt.
    /// </summary>
    public async IAsyncEnumerable<ChatStreamChunk> ChatStreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var bericht = new HttpRequestMessage(HttpMethod.Post, "/api/chat/stream")
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };

        HttpResponseMessage? response = null;
        string? fout = null;

        try
        {
            response = await httpClient.SendAsync(bericht, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            fout = $"Could not reach the AI broker: {ex.Message}";
        }

        if (fout is not null || response is null)
        {
            yield return new ChatStreamChunk(Done: true, ErrorMessage: fout ?? "No response from the AI broker.");
            yield break;
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                yield return new ChatStreamChunk(Done: true, ErrorMessage: $"AI broker returned {(int)response.StatusCode}.");
                yield break;
            }

            using var stroom = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var lezer = new StreamReader(stroom);

            while (true)
            {
                string? regel = null;

                try
                {
                    regel = await lezer.ReadLineAsync(cancellationToken);
                }
                catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException)
                {
                    fout = $"The answer stopped halfway: {ex.Message}";
                }

                if (fout is not null)
                {
                    yield return new ChatStreamChunk(Done: true, ErrorMessage: fout);
                    yield break;
                }

                // Niets meer te lezen: de broker heeft de verbinding
                // gesloten zonder afsluitend stukje. Dan sluiten we zelf
                // af, anders blijft de aanroeper wachten.
                if (regel is null)
                {
                    yield return new ChatStreamChunk(Done: true);
                    yield break;
                }

                if (string.IsNullOrWhiteSpace(regel))
                {
                    continue;
                }

                ChatStreamChunk? stukje = null;

                try
                {
                    stukje = JsonSerializer.Deserialize<ChatStreamChunk>(regel, JsonOptions);
                }
                catch (JsonException)
                {
                    // Een onleesbare regel is geen reden om te stoppen: de
                    // volgende kan prima zijn, en het echte einde komt met
                    // Done.
                }

                if (stukje is null)
                {
                    continue;
                }

                yield return stukje;

                if (stukje.Done)
                {
                    yield break;
                }
            }
        }
    }

    public async Task<EmbedResult> EmbedAsync(EmbedRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync("/api/embed", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new EmbedResult(false, [], $"AI broker returned {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<EmbedResult>(cancellationToken);
            return result ?? new EmbedResult(false, [], "Empty response from the AI broker.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new EmbedResult(false, [], $"Could not reach the AI broker: {ex.Message}");
        }
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(AiProvider provider, string serverUrl, string model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "/api/test-connection",
                new TestConnectionRequest(provider, serverUrl, model),
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new ConnectionTestResult(false, $"AI broker returned {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<ConnectionTestResult>(cancellationToken);
            return result ?? new ConnectionTestResult(false, "Empty response from the AI broker.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ConnectionTestResult(false, $"Could not reach the AI broker: {ex.Message}");
        }
    }

    public async Task<AiSettingsResult> GetSettingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync("/api/settings", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new AiSettingsResult(false, AiSettings.Default, $"AI broker returned {(int)response.StatusCode}.");
            }

            var settings = await response.Content.ReadFromJsonAsync<AiSettings>(JsonOptions, cancellationToken);
            return settings is null
                ? new AiSettingsResult(false, AiSettings.Default, "Empty response from the AI broker.")
                : new AiSettingsResult(true, settings, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new AiSettingsResult(false, AiSettings.Default, $"Could not reach the AI broker: {ex.Message}");
        }
    }

    public async Task<AiSettingsResult> SaveSettingsAsync(AiSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            using var response = await httpClient.PutAsJsonAsync("/api/settings", settings, JsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new AiSettingsResult(false, settings, $"AI broker returned {(int)response.StatusCode}.");
            }

            var saved = await response.Content.ReadFromJsonAsync<AiSettings>(JsonOptions, cancellationToken);
            return new AiSettingsResult(true, saved ?? settings, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new AiSettingsResult(false, settings, $"Could not reach the AI broker: {ex.Message}");
        }
    }

    public async Task<ModelListResult> ListConfiguredModelsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync("/api/settings/models", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new ModelListResult(false, [], $"AI broker returned {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<ModelListResult>(JsonOptions, cancellationToken);
            return result ?? new ModelListResult(false, [], "Empty response from the AI broker.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ModelListResult(false, [], $"Could not reach the AI broker: {ex.Message}");
        }
    }

    public async Task<TokenUsageStatus> GetUsageStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync("/api/usage/status", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new TokenUsageStatus(false, $"AI broker returned {(int)response.StatusCode}.");
            }

            var status = await response.Content.ReadFromJsonAsync<TokenUsageStatus>(JsonOptions, cancellationToken);
            return status ?? new TokenUsageStatus(false, "Empty response from the AI broker.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new TokenUsageStatus(false, $"Could not reach the AI broker: {ex.Message}");
        }
    }

    public async Task<IReadOnlyList<TokenUsageWeek>> GetUsageWeeksAsync(CancellationToken cancellationToken) =>
        await HaalAsync<TokenUsageWeek>("/api/usage/weeks", cancellationToken);

    public async Task<IReadOnlyList<TokenUsageModel>> GetUsageModelsAsync(CancellationToken cancellationToken) =>
        await HaalAsync<TokenUsageModel>("/api/usage/models", cancellationToken);

    public async Task<IReadOnlyList<TokenUsageEntry>> GetUsageEntriesAsync(string week, CancellationToken cancellationToken) =>
        await HaalAsync<TokenUsageEntry>($"/api/usage/entries?week={Uri.EscapeDataString(week)}", cancellationToken);

    /// <summary>
    /// Haalt een lijst op en geeft een LEGE lijst terug als er iets misgaat.
    /// Een overzicht is geen reden om een scherm te laten struikelen: dan
    /// staat er niets in plaats van dat er niets meer werkt. Wat er wél aan
    /// de hand is, ziet de gebruiker aan de gezondheidspil.
    /// </summary>
    private async Task<IReadOnlyList<T>> HaalAsync<T>(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var lijst = await response.Content.ReadFromJsonAsync<List<T>>(JsonOptions, cancellationToken);
            return lijst ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return [];
        }
    }

    public async Task<ModelListResult> ListModelsAsync(AiProvider provider, string serverUrl, CancellationToken cancellationToken)
    {
        try
        {
            var query = $"/api/models?provider={provider}&serverUrl={Uri.EscapeDataString(serverUrl)}";
            using var response = await httpClient.GetAsync(query, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new ModelListResult(false, [], $"AI broker returned {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<ModelListResult>(cancellationToken);
            return result ?? new ModelListResult(false, [], "Empty response from the AI broker.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ModelListResult(false, [], $"Could not reach the AI broker: {ex.Message}");
        }
    }
}

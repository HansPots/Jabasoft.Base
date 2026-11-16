using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Leest en wijzigt het Goose-model via de broker (GET/PUT /api/goose). Zelfde
/// opzet als <see cref="HermesClient"/>: elke fout komt terug als mislukt
/// resultaat, nooit als uitzondering.
/// </summary>
public sealed class GooseClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Een client voor de broker op zijn standaardadres.</summary>
    public static GooseClient CreateDefault() => new(new HttpClient
    {
        BaseAddress = new Uri(AiBrokerClient.DefaultBaseUrl),
        Timeout = TimeSpan.FromSeconds(30),
    });

    public async Task<GooseResult> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync("/api/goose", cancellationToken);
            return await LeesAsync(response, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new GooseResult(false, GooseStatus.Leeg, $"Could not reach the AI broker: {ex.Message}");
        }
    }

    public async Task<GooseResult> SaveAsync(GooseInstelling instelling, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instelling);

        try
        {
            using var response = await httpClient.PutAsJsonAsync("/api/goose", instelling, JsonOptions, cancellationToken);
            return await LeesAsync(response, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new GooseResult(false, GooseStatus.Leeg, $"Could not reach the AI broker: {ex.Message}");
        }
    }

    private static async Task<GooseResult> LeesAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return new GooseResult(false, GooseStatus.Leeg, $"AI broker returned {(int)response.StatusCode}.");
        }

        var status = await response.Content.ReadFromJsonAsync<GooseStatus>(JsonOptions, cancellationToken);
        return status is null
            ? new GooseResult(false, GooseStatus.Leeg, "Empty response from the AI broker.")
            : new GooseResult(string.IsNullOrEmpty(status.Melding), status, status.Melding);
    }
}

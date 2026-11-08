using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Leest en wijzigt de Hermes-instellingen via de broker (GET/PUT
/// /api/hermes). Los van <see cref="AiBrokerClient"/> gehouden: Hermes is
/// een aparte toepassing naast de familie, geen onderdeel van de AI-aanroepen
/// zelf. Net als daar komt elke fout terug als mislukt resultaat, nooit als
/// uitzondering.
/// </summary>
public sealed class HermesClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Een client voor de broker op zijn standaardadres.</summary>
    public static HermesClient CreateDefault() => new(new HttpClient
    {
        BaseAddress = new Uri(AiBrokerClient.DefaultBaseUrl),
        Timeout = TimeSpan.FromSeconds(30),
    });

    public async Task<HermesResult> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync("/api/hermes", cancellationToken);
            return await LeesAsync(response, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new HermesResult(false, HermesStatus.Leeg, $"Could not reach the AI broker: {ex.Message}");
        }
    }

    public async Task<HermesResult> SaveAsync(HermesInstelling instelling, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instelling);

        try
        {
            using var response = await httpClient.PutAsJsonAsync("/api/hermes", instelling, JsonOptions, cancellationToken);
            return await LeesAsync(response, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new HermesResult(false, HermesStatus.Leeg, $"Could not reach the AI broker: {ex.Message}");
        }
    }

    private static async Task<HermesResult> LeesAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return new HermesResult(false, HermesStatus.Leeg, $"AI broker returned {(int)response.StatusCode}.");
        }

        var status = await response.Content.ReadFromJsonAsync<HermesStatus>(JsonOptions, cancellationToken);
        return status is null
            ? new HermesResult(false, HermesStatus.Leeg, "Empty response from the AI broker.")
            : new HermesResult(string.IsNullOrEmpty(status.Melding), status, status.Melding);
    }
}

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

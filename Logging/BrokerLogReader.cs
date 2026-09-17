using System.Net.Http.Json;

namespace Jabasoft.Base.Logging;

/// <summary>
/// Haalt de regels van de AI-broker op en schrijft ze in een lokale
/// <see cref="ActivityLog"/>, zodat een applicatie ze naast zijn eigen
/// regels kan tonen.
///
/// Via een endpoint van de broker en niet via zijn console-uitvoer: die
/// uitvoer is alleen te zien door wie het proces zelf gestart heeft,
/// terwijl de broker juist gedeeld wordt. Zo ziet ELKE app wat hij doet,
/// ook als een andere app hem aan het werk zet.
///
/// Onthoudt het hoogste volgnummer dat hij zag en vraagt de volgende keer
/// alleen wat daarna kwam - dus geen dubbele regels.
/// </summary>
public sealed class BrokerLogReader(ActivityLog target, string baseUrl = AiBroker.AiBrokerClient.DefaultBaseUrl) : IDisposable
{
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(3) };
    private readonly CancellationTokenSource _stopping = new();
    private long _lastSequence;
    private Task? _loop;

    /// <summary>Hoe vaak er bijgehaald wordt.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(2);

    public void Start() => _loop ??= Task.Run(() => LoopAsync(_stopping.Token));

    private async Task LoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var entries = await _client
                    .GetFromJsonAsync<List<ActivityEntry>>($"{baseUrl.TrimEnd('/')}/logs?since={_lastSequence}", cancellationToken)
                    .ConfigureAwait(false);

                foreach (var entry in entries ?? [])
                {
                    _lastSequence = Math.Max(_lastSequence, entry.Sequence);
                    target.Add(entry.Source, entry.Message);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception)
            {
                // Broker draait even niet of is nog aan het opstarten. Geen
                // regel daarover in het log: dat zou elke paar seconden
                // herhalen en het overzicht onbruikbaar maken.
            }

            try
            {
                await Task.Delay(Interval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    public void Dispose()
    {
        _stopping.Cancel();
        _stopping.Dispose();
        _client.Dispose();
    }
}

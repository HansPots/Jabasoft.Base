using Jabasoft.Base.Health;

namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Zorgt dat de AI-broker draait en bereikbaar is.
///
/// Draait hij al, dan is dit in een oogwenk klaar. Draait hij niet, dan
/// wordt hij hier OPGESTART - de broker is een gedeelde voorziening waar
/// meerdere applicaties op leunen, dus wie hem als eerste nodig heeft zet
/// hem aan. Zolang dat loopt blijft de pil oranje met "Starting AI
/// broker": het is nog niet fout, het duurt alleen even.
///
/// Komt hij binnen de wachttijd van AiBrokerProcessLauncher niet omhoog,
/// dan pas rood.
/// </summary>
public sealed class AiBrokerHealthCheck(
    string baseUrl = AiBrokerClient.DefaultBaseUrl,
    string projectPath = @"C:\Repos\Jabasoft.Broker",
    TimeSpan? probeTimeout = null) : IHealthCheck
{
    private readonly TimeSpan _probeTimeout = probeTimeout ?? TimeSpan.FromSeconds(2);

    /// <inheritdoc />
    public string Name => "AI broker";

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);

        if (await IsReachableAsync(cancellationToken).ConfigureAwait(false))
        {
            return true;
        }

        progress.Report("Starting AI broker");
        await AiBrokerProcessLauncher
            .EnsureRunningAsync(baseUrl, projectPath, cancellationToken)
            .ConfigureAwait(false);

        // EnsureRunningAsync wacht zelf tot hij antwoordt of tot zijn
        // wachttijd om is; deze laatste probe zegt welke van de twee.
        return await IsReachableAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> IsReachableAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = _probeTimeout };

        try
        {
            using var response = await client
                .GetAsync($"{baseUrl.TrimEnd('/')}/health", cancellationToken)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Time-out van de HttpClient: niet bereikbaar, geen fout om te melden.
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
}

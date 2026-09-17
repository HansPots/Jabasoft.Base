namespace Jabasoft.Base.Health;

/// <summary>
/// Loopt de controles van een applicatie af en meldt onderweg wat hij aan
/// het doen is. Hier in Jabasoft.Base omdat het bewaken voor elke app
/// hetzelfde werkt; alleen WELKE controles er zijn verschilt, en die geeft
/// de app zelf mee.
///
/// Verloop: oranje "Checking &lt;naam&gt;" per controle, en aan het eind
/// groen "Ready" of rood "Error". Zonder controles is het meteen groen -
/// een app die niets nodig heeft, is klaar.
///
/// Net als SystemStatsPoller komt <see cref="Changed"/> terug op de draad
/// waarop de monitor is GEMAAKT als die een SynchronizationContext heeft.
/// In een WPF-app is dat de UI-draad, dus een handler mag rechtstreeks aan
/// de schermelementen komen.
/// </summary>
public sealed class HealthMonitor
{
    /// <summary>De tekst bij groen.</summary>
    public const string ReadyMessage = "Ready";

    /// <summary>De tekst bij rood. Wat er precies mis is komt elders; zie de toelichting op HealthStatus.</summary>
    public const string ErrorMessage = "Error";

    private readonly IReadOnlyList<IHealthCheck> _checks;
    private readonly SynchronizationContext? _context;

    public HealthMonitor(IEnumerable<IHealthCheck> checks)
    {
        ArgumentNullException.ThrowIfNull(checks);

        _checks = checks.ToList();
        _context = SynchronizationContext.Current;
    }

    /// <summary>Gaat af bij elke stap: per controle één keer oranje, en één keer aan het eind.</summary>
    public event EventHandler<HealthStatus>? Changed;

    /// <summary>De laatste stand, ook voor wie zich pas na de start aanmeldt.</summary>
    public HealthStatus Current { get; private set; } = new(HealthState.Checking, "Starting");

    /// <summary>
    /// Loopt alle controles langs. Eén die faalt of een uitzondering
    /// gooit zet het geheel op rood en stopt de rest - er is toch iets mis,
    /// en doorgaan zou de melding alleen maar overschrijven.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Een controle die onderweg iets te melden heeft, blijft ORANJE:
        // bezig is niet hetzelfde als fout. Geen Progress<T> hier, want die
        // post zelf al naar een SynchronizationContext en Report doet dat
        // ook - dat zou het dubbel doen.
        var progress = new CheckProgress(message => Report(new HealthStatus(HealthState.Checking, message)));

        foreach (var check in _checks)
        {
            Report(new HealthStatus(HealthState.Checking, $"Checking {check.Name}"));

            bool ok;
            try
            {
                ok = await check.IsHealthyAsync(progress, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                ok = false;
            }

            if (!ok)
            {
                Report(new HealthStatus(HealthState.Failed, ErrorMessage));
                return;
            }
        }

        Report(new HealthStatus(HealthState.Healthy, ReadyMessage));
    }

    private sealed class CheckProgress(Action<string> onReport) : IProgress<string>
    {
        public void Report(string value) => onReport(value);
    }

    private void Report(HealthStatus status)
    {
        Current = status;

        if (_context is null)
        {
            Changed?.Invoke(this, status);
            return;
        }

        _context.Post(_ => Changed?.Invoke(this, status), null);
    }
}

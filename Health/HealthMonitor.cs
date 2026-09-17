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

    /// <summary>Gaat af zodra er van een controle een uitslag bij komt - voor het gezondheidsoverzicht, dat de hele rij toont.</summary>
    public event EventHandler<IReadOnlyList<HealthCheckResult>>? ResultsChanged;

    /// <summary>De uitslag per controle, in dezelfde volgorde als ze meegegeven zijn.</summary>
    public IReadOnlyList<HealthCheckResult> Results { get; private set; } = [];

    /// <summary>Loopt er al een ronde? Dan doet een tweede aanroep niets.</summary>
    private int _loopt;

    /// <summary>
    /// Loopt ALLE controles langs, ook als er onderweg een faalt. Dat is met
    /// opzet: het gezondheidsoverzicht moet laten zien wat er wel en niet
    /// draait, en dan helpt het niet als de rij halverwege afbreekt. De pil
    /// wordt rood zodra er één faalt, maar de rest wordt gewoon nog
    /// gecontroleerd.
    ///
    /// Let op wat dat betekent: controles die op elkaar leunen (zonder
    /// broker valt er over de modellen niets te zeggen) melden dan allemaal
    /// hun eigen fout. De bovenste is de echte oorzaak.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Twee rondes tegelijk zouden elkaars uitslagen overschrijven - dat
        // kan zomaar gebeuren als je op "opnieuw controleren" drukt terwijl
        // de eerste ronde nog loopt.
        if (Interlocked.Exchange(ref _loopt, 1) == 1)
        {
            return;
        }

        try
        {
            // Een controle die onderweg iets te melden heeft, blijft ORANJE:
            // bezig is niet hetzelfde als fout. Geen Progress<T> hier, want die
            // post zelf al naar een SynchronizationContext en Report doet dat
            // ook - dat zou het dubbel doen.
            var progress = new CheckProgress(message => Report(new HealthStatus(HealthState.Checking, message)));

            // Alles begint op "nog niet aan de beurt", zodat het overzicht
            // meteen de hele rij laat zien in plaats van hem regel voor
            // regel te laten aangroeien.
            var uitslagen = _checks
                .Select(check => new HealthCheckResult(check.Name, HealthState.Checking, "Nog niet gecontroleerd", TimeSpan.Zero))
                .ToList();

            MeldUitslagen(uitslagen);

            var allesGoed = true;

            for (var i = 0; i < _checks.Count; i++)
            {
                var check = _checks[i];
                Report(new HealthStatus(HealthState.Checking, $"Checking {check.Name}"));

                uitslagen[i] = uitslagen[i] with { Message = "Bezig" };
                MeldUitslagen(uitslagen);

                var start = System.Diagnostics.Stopwatch.GetTimestamp();
                HealthCheckOutcome uitkomst;

                try
                {
                    uitkomst = await check.CheckAsync(progress, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    uitkomst = HealthCheckOutcome.Fout(ex.Message);
                }

                uitslagen[i] = new HealthCheckResult(
                    check.Name,
                    uitkomst.Healthy ? HealthState.Healthy : HealthState.Failed,
                    uitkomst.Message,
                    System.Diagnostics.Stopwatch.GetElapsedTime(start));

                MeldUitslagen(uitslagen);

                allesGoed &= uitkomst.Healthy;
            }

            Report(allesGoed
                ? new HealthStatus(HealthState.Healthy, ReadyMessage)
                : new HealthStatus(HealthState.Failed, ErrorMessage));
        }
        finally
        {
            Interlocked.Exchange(ref _loopt, 0);
        }
    }

    /// <summary>Een kopie naar buiten, zodat een luisteraar niet in de lijst kan roeren die hier nog bijgewerkt wordt.</summary>
    private void MeldUitslagen(List<HealthCheckResult> uitslagen)
    {
        var kopie = uitslagen.ToList();
        Results = kopie;

        if (_context is null)
        {
            ResultsChanged?.Invoke(this, kopie);
            return;
        }

        _context.Post(_ => ResultsChanged?.Invoke(this, kopie), null);
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

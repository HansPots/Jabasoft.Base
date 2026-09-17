namespace Jabasoft.Base.SystemStats;

/// <summary>
/// Meet op een instelbaar interval en meldt elke uitkomst via
/// <see cref="Updated"/>. Hier, naast de service zelf, zodat elke
/// applicatie alleen nog hoeft te zeggen wat hij met de getallen doet -
/// het meten EN het herhalen staan op één plek.
///
/// Draai er één per applicatie en houd 'm in leven zolang het venster
/// leeft: <see cref="WindowsSystemStatsService"/> onthoudt de vorige
/// CPU-meting om er een percentage uit te rekenen, dus elke keer een nieuwe
/// maken zou altijd 0% opleveren.
///
/// Het event komt terug op de draad waarop de poller is GEMAAKT, als die
/// een SynchronizationContext heeft. In een WPF-app is dat de UI-draad,
/// dus een handler mag rechtstreeks aan de schermelementen komen zonder
/// Dispatcher.Invoke. Is er geen context, dan komt het op een threadpool-
/// draad binnen.
/// </summary>
public sealed class SystemStatsPoller : IDisposable
{
    private readonly ISystemStatsService _service;
    private readonly SynchronizationContext? _context;
    private readonly CancellationTokenSource _stopping = new();
    private TimeSpan _interval;
    private Task? _loop;

    public SystemStatsPoller(ISystemStatsService service, TimeSpan interval)
    {
        ArgumentNullException.ThrowIfNull(service);

        _service = service;
        _context = SynchronizationContext.Current;
        Interval = interval;
    }

    /// <summary>Gaat af na elke meting.</summary>
    public event EventHandler<SystemStatsSnapshot>? Updated;

    /// <summary>
    /// Hoe vaak er gemeten wordt. Tussentijds wijzigen mag; het gaat in
    /// zodra de lopende wachttijd voorbij is, dus uiterlijk na één oude
    /// periode. Minder dan een seconde wordt opgetrokken naar een seconde -
    /// nvidia-smi start een proces en dat wil je niet honderden keren per
    /// minuut doen.
    /// </summary>
    public TimeSpan Interval
    {
        get => _interval;
        set => _interval = value < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : value;
    }

    /// <summary>Begint te meten. Meteen een eerste meting, daarna elk interval opnieuw.</summary>
    public void Start() => _loop ??= Task.Run(() => LoopAsync(_stopping.Token));

    private async Task LoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var snapshot = await _service.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
                Report(snapshot);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception)
            {
                // Eén mislukte meting mag de meter niet voorgoed stilzetten -
                // volgende ronde gewoon opnieuw proberen.
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

    private void Report(SystemStatsSnapshot snapshot)
    {
        if (_context is null)
        {
            Updated?.Invoke(this, snapshot);
            return;
        }

        _context.Post(_ => Updated?.Invoke(this, snapshot), null);
    }

    public void Dispose()
    {
        _stopping.Cancel();
        _stopping.Dispose();
    }
}

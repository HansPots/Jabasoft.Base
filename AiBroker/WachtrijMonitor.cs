namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Houdt de wachtrij van de broker bij voor het scherm: vraagt hem elke
/// seconde op zolang er iemand meekijkt, en meldt het als hij veranderd is.
///
/// Statisch en één per applicatie, zelfde opzet als <see cref="TokenTicker"/>:
/// de footer van elk onderdeel kijkt naar dezelfde lijst, en er gaat dus ook
/// maar één vraag per seconde naar de broker, hoeveel footers er ook zijn.
/// Kijkt er niemand (meer), dan stopt het ophalen vanzelf.
/// </summary>
public static class WachtrijMonitor
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private static readonly IAiBrokerClient Broker = AiBrokerClient.CreateDefault(timeout: TimeSpan.FromSeconds(3));

    private static readonly Lock Slot = new();

    private static EventHandler? _changed;
    private static CancellationTokenSource? _lus;
    private static string _vorige = string.Empty;

    /// <summary>De laatst opgehaalde lijst: eerst wat loopt, dan wat wacht (op volgorde), dan wat net klaar is.</summary>
    public static IReadOnlyList<WachtrijItem> Current { get; private set; } = [];

    /// <summary>
    /// Gaat af als de lijst anders is dan de vorige keer. Komt van een
    /// achtergronddraad - wie het scherm bijwerkt gaat via de Dispatcher.
    /// Aanmelden start het ophalen, het laatste afmelden stopt het.
    /// </summary>
    public static event EventHandler? Changed
    {
        add
        {
            lock (Slot)
            {
                _changed += value;
                if (_lus is null)
                {
                    _lus = new CancellationTokenSource();
                    _ = HaalAsync(_lus.Token);
                }
            }
        }
        remove
        {
            lock (Slot)
            {
                _changed -= value;
                if (_changed is null && _lus is not null)
                {
                    _lus.Cancel();
                    _lus.Dispose();
                    _lus = null;
                }
            }
        }
    }

    /// <summary>Annuleert een vraag en haalt de lijst meteen opnieuw op, zodat het scherm niet een seconde achterloopt.</summary>
    public static async Task<bool> AnnuleerAsync(long id)
    {
        var gelukt = await Broker.CancelQueueItemAsync(id, CancellationToken.None).ConfigureAwait(false);
        await VernieuwAsync(CancellationToken.None).ConfigureAwait(false);
        return gelukt;
    }

    private static async Task HaalAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(Interval);

        try
        {
            do
            {
                await VernieuwAsync(cancellationToken).ConfigureAwait(false);
            }
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            // Niemand kijkt meer mee.
        }
    }

    private static async Task VernieuwAsync(CancellationToken cancellationToken)
    {
        var lijst = await Broker.GetQueueAsync(cancellationToken).ConfigureAwait(false);

        // Alleen melden als er echt iets veranderd is: elke seconde het scherm
        // opnieuw opbouwen voor dezelfde lijst laat een open lijstje flikkeren.
        var vingerafdruk = string.Join('|', lijst.Select(item => $"{item.Id}:{item.Status}:{item.Plek}:{item.Inlezen:0.00}:{item.Tokens}"));

        EventHandler? melden;
        lock (Slot)
        {
            if (vingerafdruk == _vorige)
            {
                return;
            }

            _vorige = vingerafdruk;
            Current = lijst;
            melden = _changed;
        }

        melden?.Invoke(null, EventArgs.Empty);
    }
}

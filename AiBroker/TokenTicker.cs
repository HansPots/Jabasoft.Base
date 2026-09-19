namespace Jabasoft.Base.AiBroker;

/// <summary>
/// De teller die meeloopt terwijl een model antwoord geeft.
///
/// Het TOKENS-blok laat normaal zien wat er in de database staat, en dat
/// komt er pas bij als een aanroep KLAAR is. Tijdens het nadenken van het
/// model gebeurde er dus zichtbaar niets. Deze teller vult dat gat: elk
/// stukje antwoord dat binnenkomt telt mee, zodat je ziet dat er gewerkt
/// wordt.
///
/// Het is met opzet een SCHATTING zolang het loopt - een stukje is
/// meestal één token, maar niet gegarandeerd. Zodra het antwoord klaar is
/// komt het echte aantal van de server, en dat gaat naar de database. De
/// teller is dus een levensteken, geen boekhouding.
///
/// Statisch, net als <see cref="Logging.ActivityLog.Shared"/>: er is er
/// één per applicatie, en het scherm dat de vraag stelt en het blok dat
/// het toont hoeven elkaar niet te kennen.
/// </summary>
public static class TokenTicker
{
    private static readonly Lock Slot = new();

    /// <summary>Gaat af bij elke verandering - starten, optellen, stoppen.</summary>
    public static event EventHandler? Changed;

    /// <summary>Of er op dit moment een antwoord binnenkomt.</summary>
    public static bool Running { get; private set; }

    /// <summary>Hoeveel er tot nu toe geteld is in de lopende aanroep.</summary>
    public static long Tokens { get; private set; }

    /// <summary>Welk model er aan het woord is, om bij de teller te kunnen zetten.</summary>
    public static string Model { get; private set; } = string.Empty;

    /// <summary>Er begint een antwoord. Een eventueel vorig getal gaat op nul.</summary>
    public static void Start(string model)
    {
        lock (Slot)
        {
            Running = true;
            Tokens = 0;
            Model = model ?? string.Empty;
        }

        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Er is weer een stukje antwoord binnen.</summary>
    public static void Add(long aantal = 1)
    {
        if (aantal <= 0)
        {
            return;
        }

        lock (Slot)
        {
            if (!Running)
            {
                return;
            }

            Tokens += aantal;
        }

        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Het antwoord is klaar. Geeft de server een echt aantal op, dan
    /// vervangt dat de schatting - zo eindigt de teller op het getal dat
    /// ook in de database komt te staan, in plaats van er net naast.
    /// </summary>
    public static void Stop(long werkelijkAantal = 0)
    {
        lock (Slot)
        {
            Running = false;

            if (werkelijkAantal > 0)
            {
                Tokens = werkelijkAantal;
            }
        }

        Changed?.Invoke(null, EventArgs.Empty);
    }
}

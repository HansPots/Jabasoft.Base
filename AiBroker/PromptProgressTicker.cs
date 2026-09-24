namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Hoe ver LM Studio is met het INLEZEN van de prompt, vóór het model met
/// antwoorden begint - zie <see cref="ChatStreamChunk.PromptProgress"/>.
///
/// Zelfde opzet als <see cref="TokenTicker"/>: statisch, één per
/// applicatie, zodat het scherm dat de vraag stelt en de pil die het toont
/// elkaar niet hoeven te kennen. Anders dan TokenTicker is dit geen
/// schatting die aangroeit - het is het percentage zoals de server het
/// doorgeeft, dus Update zet de stand gewoon neer in plaats van erbij op
/// te tellen.
///
/// Blijft <see cref="Active"/> op false staan (Ollama, of een provider die
/// dit niet geeft), dan is er niets te tonen - de pil valt terug op zijn
/// stille stand in plaats van te blijven hangen op de laatste waarde.
/// </summary>
public static class PromptProgressTicker
{
    private static readonly Lock Slot = new();

    /// <summary>Gaat af bij elke verandering - starten, bijwerken, stoppen.</summary>
    public static event EventHandler? Changed;

    /// <summary>Of er op dit moment een percentage te tonen is.</summary>
    public static bool Active { get; private set; }

    /// <summary>De laatste stand, 0-1.</summary>
    public static double Progress { get; private set; }

    /// <summary>Er begint een aanroep. Nog geen percentage bekend, dus Active blijft false tot de eerste Update.</summary>
    public static void Start()
    {
        lock (Slot)
        {
            Active = false;
            Progress = 0;
        }

        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Een nieuwe stand van de server - vervangt de vorige, telt niet op.</summary>
    public static void Update(double progress)
    {
        lock (Slot)
        {
            Active = true;
            Progress = Math.Clamp(progress, 0, 1);
        }

        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>De aanroep is klaar (of mislukt) - niets meer te tonen.</summary>
    public static void Stop()
    {
        lock (Slot)
        {
            Active = false;
            Progress = 0;
        }

        Changed?.Invoke(null, EventArgs.Empty);
    }
}

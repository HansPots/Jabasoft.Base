using System.Collections.Concurrent;

namespace Jabasoft.Base.Logging;

/// <summary>
/// Eén regel in het activiteitenoverzicht. Het volgnummer is er zodat een
/// afnemer kan vragen "alles NA dit nummer" en niets dubbel binnenkrijgt -
/// de broker geeft zijn regels zo af aan de applicaties.
/// </summary>
public sealed record ActivityEntry(long Sequence, DateTimeOffset Time, string Source, string Message);

/// <summary>
/// Houdt de laatste regels vast van wat een applicatie en zijn
/// achtergrondprocessen doen. In Jabasoft.Base omdat zowel de apps als de
/// broker erop schrijven, en elke app hetzelfde Activity-blok toont.
///
/// Alleen in het geheugen en met een bovengrens: dit is een kijkvenster op
/// wat er NU gebeurt, geen archief. Oudere regels vallen er vanzelf uit.
/// </summary>
public sealed class ActivityLog(int capacity = 200)
{
    /// <summary>De opvang die een applicatie deelt met alles wat erin draait.</summary>
    public static ActivityLog Shared { get; } = new();

    private readonly ConcurrentQueue<ActivityEntry> _entries = new();
    private long _sequence;

    /// <summary>Gaat af bij elke nieuwe regel. Komt op de draad van de schrijver - een UI-afnemer moet zelf naar zijn eigen draad springen.</summary>
    public event EventHandler<ActivityEntry>? Added;

    /// <summary>Schrijft een regel. <paramref name="source"/> is waar hij vandaan komt, bijvoorbeeld "app" of "broker".</summary>
    public ActivityEntry Add(string source, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(message);

        var entry = new ActivityEntry(Interlocked.Increment(ref _sequence), DateTimeOffset.Now, source, message);
        _entries.Enqueue(entry);

        while (_entries.Count > capacity && _entries.TryDequeue(out _))
        {
            // Ouderste regels vallen eruit zodra we boven de grens komen.
        }

        Added?.Invoke(this, entry);
        return entry;
    }

    /// <summary>Alles wat er nu in zit, oudste eerst.</summary>
    public IReadOnlyList<ActivityEntry> Snapshot() => [.. _entries];

    /// <summary>Alles met een hoger volgnummer dan <paramref name="since"/> - voor wie regelmatig bijhaalt.</summary>
    public IReadOnlyList<ActivityEntry> Since(long since) => [.. _entries.Where(entry => entry.Sequence > since)];
}

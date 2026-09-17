namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Eén vastgelegde AI-aanroep: wanneer, door welke applicatie, met welk
/// model, en hoeveel tokens het kostte.
///
/// Komt uit de gedeelde tabel JabasoftBase.dbo.TokenUsageEntries. Een rij
/// per aanroep en geen lopende teller: alleen zo kun je achteraf per week,
/// per applicatie of per model kijken.
/// </summary>
public sealed record TokenUsageEntry(
    Guid Id,
    string Application,
    DateTimeOffset Timestamp,
    string? Model,
    long PromptTokens,
    long CompletionTokens,
    long TotalTokens);

/// <summary>
/// Het totaal van één week (maandag tot en met zondag, ISO-telling).
/// <see cref="Week"/> is de sleutel waarmee de losse regels van die week op
/// te vragen zijn, bijvoorbeeld "2026-W38".
/// </summary>
public sealed record TokenUsageWeek(
    string Week,
    DateTime Start,
    DateTime End,
    long PromptTokens,
    long CompletionTokens,
    long TotalTokens,
    int Calls);

/// <summary>
/// Of de tokentabel bereikbaar is. Zegt er ook bij WAT er mis is, zodat de
/// gezondheidscontrole van een applicatie dat in het activiteitenlog kan
/// zetten in plaats van alleen "Error".
/// </summary>
public sealed record TokenUsageStatus(bool Available, string? Message);

/// <summary>
/// Het totaal van één model, over alle weken en applicaties heen. Voor het
/// tokenblok in de header: welk model kost je de meeste tokens.
/// </summary>
public sealed record TokenUsageModel(string Model, long TotalTokens, int Calls);

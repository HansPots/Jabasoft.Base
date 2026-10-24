namespace Jabasoft.Base.AiBroker;

/// <summary>Waar een vraag in de wachtrij van de broker staat - zie <see cref="WachtrijItem"/>.</summary>
public enum WachtrijStatus
{
    /// <summary>Aangemeld, wacht tot de vraag ervoor klaar is.</summary>
    Wachtend,

    /// <summary>Aan de beurt: model laden, prompt inlezen of antwoorden.</summary>
    Bezig,

    /// <summary>Klaar, met antwoord.</summary>
    Klaar,

    /// <summary>Klaar, maar met een fout.</summary>
    Mislukt,

    /// <summary>Weggehaald of afgebroken met de Annuleren-knop.</summary>
    Geannuleerd,
}

/// <summary>
/// Eén vraag aan een model, zoals de broker hem in zijn wachtrij bijhoudt.
/// De broker laat per server één vraag tegelijk door (zie RequestGate); de
/// rest wacht op volgorde van binnenkomst. Elke applicatie haalt deze lijst
/// op met <see cref="IAiBrokerClient.GetQueueAsync"/> om hem in de footer te
/// tonen.
/// </summary>
/// <param name="Id">Kort volgnummer, om een vraag mee te annuleren.</param>
/// <param name="Applicatie">Wie de vraag stelde, bijvoorbeeld "LocalAiStudio".</param>
/// <param name="Onderdeel">Van welke pagina, als de applicatie dat meegaf (bijvoorbeeld "Daily chat").</param>
/// <param name="Model">Het model dat de vraag krijgt.</param>
/// <param name="Soort">"chat" of "embedding".</param>
/// <param name="Plek">Hoeveelste in de rij (1 = als volgende aan de beurt); 0 als hij niet (meer) wacht.</param>
/// <param name="Inlezen">Hoe ver het inlezen van de prompt is (0-1), als de server dat doorgeeft.</param>
/// <param name="Tokens">Hoeveel stukjes antwoord er al binnen zijn - ongeveer het aantal tokens.</param>
public sealed record WachtrijItem(
    long Id,
    string Applicatie,
    string? Onderdeel,
    string Model,
    string Soort,
    WachtrijStatus Status,
    int Plek,
    double? Inlezen,
    long Tokens,
    DateTimeOffset Aangemeld,
    DateTimeOffset? Gestart,
    DateTimeOffset? Afgerond,
    string? Fout);

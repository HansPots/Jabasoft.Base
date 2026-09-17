namespace Jabasoft.Base.Health;

/// <summary>
/// De drie toestanden die het bolletje in de SYSTEM HEALTH-pil kan hebben.
/// Meer zijn het er niet: de bedoeling is dat je in één oogopslag ziet of
/// je verder kunt, niet dat je hier een verslag leest.
/// </summary>
public enum HealthState
{
    /// <summary>Oranje: er wordt nu gecontroleerd.</summary>
    Checking,

    /// <summary>Groen: alles staat klaar.</summary>
    Healthy,

    /// <summary>Rood: er is iets mis.</summary>
    Failed,
}

/// <summary>
/// Wat de pil op dit moment toont: een toestand en één korte regel. Die
/// regel is bewust globaal ("Checking AI broker", "Ready", "Error") - wát
/// er precies mis is hoort ergens anders thuis, niet in een pil van 24 px.
/// </summary>
public sealed record HealthStatus(HealthState State, string Message);

/// <summary>
/// De uitslag van één controle: of het goed is, en in één regel waarom.
/// Die regel is voor het gezondheidsoverzicht, waar je wilt zien wát er
/// draait ("LM Studio op http://localhost:1234 - 5 modellen") en niet
/// alleen dat het goed is.
/// </summary>
public sealed record HealthCheckOutcome(bool Healthy, string Message)
{
    public static HealthCheckOutcome Ok(string message) => new(true, message);

    public static HealthCheckOutcome Fout(string message) => new(false, message);
}

/// <summary>
/// Eén ding dat bij het opstarten gecontroleerd wordt. Elke applicatie
/// levert zijn eigen lijstje: de een heeft een AI-broker nodig, de ander
/// een database.
/// </summary>
public interface IHealthCheck
{
    /// <summary>Korte naam, komt achter "Checking " te staan in de pil.</summary>
    string Name { get; }

    /// <summary>
    /// Doet de controle en zegt hoe het afliep. Duurt het lang en gebeurt
    /// er onderweg iets dat de gebruiker mag weten - een voorziening die
    /// eerst opgestart wordt bijvoorbeeld - meld dat dan via
    /// <paramref name="progress"/>. Dat blijft ORANJE: het is nog niet
    /// fout, het is nog bezig.
    /// </summary>
    Task<HealthCheckOutcome> CheckAsync(IProgress<string> progress, CancellationToken cancellationToken);
}

/// <summary>
/// Wat er van één controle bekend is nadat de monitor hem langs is
/// geweest: de naam, hoe het afliep, de toelichting en hoe lang het duurde.
///
/// <see cref="HealthState.Checking"/> betekent hier twee dingen, en dat is
/// precies wat je wilt zien in het overzicht: hij is nu bezig, of hij is
/// nog niet aan de beurt geweest.
/// </summary>
public sealed record HealthCheckResult(string Name, HealthState State, string Message, TimeSpan Duration);

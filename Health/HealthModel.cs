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
/// Eén ding dat bij het opstarten gecontroleerd wordt. Elke applicatie
/// levert zijn eigen lijstje: de een heeft een AI-broker nodig, de ander
/// een database.
/// </summary>
public interface IHealthCheck
{
    /// <summary>Korte naam, komt achter "Checking " te staan in de pil.</summary>
    string Name { get; }

    /// <summary>
    /// Waar of niet waar. Duurt het lang en gebeurt er onderweg iets dat
    /// de gebruiker mag weten - een voorziening die eerst opgestart wordt
    /// bijvoorbeeld - meld dat dan via <paramref name="progress"/>. Dat
    /// blijft ORANJE: het is nog niet fout, het is nog bezig.
    /// </summary>
    Task<bool> IsHealthyAsync(IProgress<string> progress, CancellationToken cancellationToken);
}

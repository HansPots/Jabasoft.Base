using Jabasoft.Base.Health;
using Jabasoft.Base.Logging;

namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Controleert of de INGESTELDE modellen ook echt aanwezig zijn op de
/// AI-server. Hoort achter <see cref="AiBrokerHealthCheck"/> in de rij: de
/// broker moet eerst draaien, want die levert de lijst.
///
/// Wie wat doet: de BROKER weet welke modellen er aanwezig zijn (hij vraagt
/// het aan LM Studio of Ollama en houdt dat even vast), de APPLICATIE legt
/// die lijst naast de ingestelde namen. Klopt er iets niet, dan wordt de
/// pil rood met "Error".
///
/// Wát er niet klopt past niet in een pil van 24 px en gaat daarom naar het
/// activiteitenlog - dat staat in beeld naast de pil, dus je ziet het
/// meteen.
/// </summary>
public sealed class AiModelsHealthCheck(IAiBrokerClient client, ActivityLog? log = null) : IHealthCheck
{
    private readonly ActivityLog _log = log ?? ActivityLog.Shared;

    /// <inheritdoc />
    public string Name => "AI models";

    /// <inheritdoc />
    public async Task<HealthCheckOutcome> CheckAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);

        var settings = await client.GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (!settings.Success)
        {
            return Meld(HealthCheckOutcome.Fout($"Instelling niet op te halen: {settings.ErrorMessage}"));
        }

        var ingesteld = settings.Settings.ConfiguredModels;
        if (ingesteld.Count == 0)
        {
            // Niets ingesteld is geen geldige toestand: er is dan geen model
            // om mee te werken.
            return Meld(HealthCheckOutcome.Fout("Geen model ingesteld - zie Settings, kaart AI"));
        }

        var aanwezig = await client.ListConfiguredModelsAsync(cancellationToken).ConfigureAwait(false);
        if (!aanwezig.Success)
        {
            return Meld(HealthCheckOutcome.Fout($"Modellijst niet op te halen bij {settings.Settings.ActiveServerUrl}: {aanwezig.ErrorMessage}"));
        }

        var ontbreekt = ingesteld
            .Where(model => !aanwezig.Models.Contains(model, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return ontbreekt.Count > 0
            ? Meld(HealthCheckOutcome.Fout($"Niet aanwezig op {settings.Settings.Provider}: {string.Join(", ", ontbreekt)}"))
            : Meld(HealthCheckOutcome.Ok($"Aanwezig: {string.Join(", ", ingesteld)}"));
    }

    /// <summary>
    /// Zet de uitslag ook in het activiteitenlog. Dat blijft nuttig naast
    /// het gezondheidsoverzicht: in het log zie je de volgorde waarin het
    /// bij het opstarten gebeurde, met een tijdstip erbij.
    /// </summary>
    private HealthCheckOutcome Meld(HealthCheckOutcome uitkomst)
    {
        _log.Add("app", uitkomst.Healthy
            ? $"AI-modellen: {uitkomst.Message}"
            : $"AI-modellen niet in orde: {uitkomst.Message}");

        return uitkomst;
    }
}

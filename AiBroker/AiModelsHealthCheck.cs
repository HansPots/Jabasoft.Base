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
    public async Task<bool> IsHealthyAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);

        var settings = await client.GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (!settings.Success)
        {
            _log.Add("app", $"AI-instelling niet op te halen: {settings.ErrorMessage}");
            return false;
        }

        var ingesteld = settings.Settings.ConfiguredModels;
        if (ingesteld.Count == 0)
        {
            // Niets ingesteld is geen geldige toestand: er is dan geen model
            // om mee te werken. Rood dus, met in het log waar je het zet.
            _log.Add("app", "Geen AI-model ingesteld - zie Settings, kaart AI.");
            return false;
        }

        progress.Report($"Checking {settings.Settings.Provider}");

        var aanwezig = await client.ListConfiguredModelsAsync(cancellationToken).ConfigureAwait(false);
        if (!aanwezig.Success)
        {
            _log.Add("app", $"Modellijst niet op te halen bij {settings.Settings.ActiveServerUrl}: {aanwezig.ErrorMessage}");
            return false;
        }

        var ontbreekt = ingesteld
            .Where(model => !aanwezig.Models.Contains(model, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (ontbreekt.Count > 0)
        {
            _log.Add("app", $"Model niet aanwezig op {settings.Settings.Provider}: {string.Join(", ", ontbreekt)}");
            return false;
        }

        _log.Add("app", $"AI-modellen aanwezig: {string.Join(", ", ingesteld)}");
        return true;
    }
}

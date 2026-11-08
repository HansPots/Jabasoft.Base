namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Of de sleutel voor de gekozen Hermes-provider er is. De sleutel zelf komt
/// nooit over de lijn: de broker kijkt alleen OF hij ingesteld is.
/// </summary>
public enum HermesSleutelStatus
{
    /// <summary>Niet vast te stellen (bijvoorbeeld een provider die we niet kennen).</summary>
    Onbekend,

    /// <summary>Er staat een sleutel of login voor deze provider.</summary>
    Ingesteld,

    /// <summary>Nodig, maar er staat niets: vul hem in Hermes zelf in.</summary>
    Ontbreekt,

    /// <summary>Deze provider heeft geen sleutel nodig (lokaal, zoals LM Studio).</summary>
    NietNodig,
}

/// <summary>Een model op de lokale server (LM Studio) met de grootste context die het aankan.</summary>
public sealed record HermesLokaalModel(string Naam, int MaxContext);

/// <summary>
/// Hoe Hermes Agent er op deze machine bij staat, zoals de broker het uit
/// Hermes' eigen config.yaml (en de map eromheen) leest. Alleen de dingen die
/// Jabasoft instelt plus wat status - de rest van Hermes' instellingen
/// (chatkanalen, persoonlijkheid, skills) blijft in de Hermes-app.
/// </summary>
public sealed record HermesStatus(
    bool Geinstalleerd,
    bool GatewayDraait,
    string HermesMap,
    string Provider,
    string Model,
    HermesSleutelStatus Sleutel,
    string LokaalModel,
    string LokaalServerUrl,
    bool JabasoftMcp,
    IReadOnlyList<HermesLokaalModel> LokaleModellen,
    string? Melding = null)
{
    /// <summary>Wat Hermes minimaal aan context eist van een model dat als agent meedraait.</summary>
    public const int MinimaleContext = 64_000;

    public static HermesStatus Leeg { get; } = new(false, false, string.Empty, string.Empty, string.Empty, HermesSleutelStatus.Onbekend, string.Empty, string.Empty, false, []);
}

/// <summary>
/// Wat Jabasoft in Hermes wijzigt. Leeg <see cref="LokaalModel"/> haalt de
/// lokale bijtaken en de reserve weer weg (Hermes valt dan terug op zijn
/// hoofdmodel).
/// </summary>
public sealed record HermesInstelling(string Provider, string Model, string LokaalModel, bool JabasoftMcp);

public sealed record HermesResult(bool Success, HermesStatus Status, string? ErrorMessage);

namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Hoe Goose (Block) er op deze machine bij staat, zoals de broker het uit
/// Goose' eigen config leest. Alleen het model dat Jabasoft instelt plus wat
/// status - de rest (extensies, modus, werkmap) blijft in Goose zelf.
/// <see cref="LokaleModellen"/> zijn dezelfde LM Studio-modellen als op de
/// Hermes-kaart, met context en of ze afbeeldingen lezen.
/// </summary>
public sealed record GooseStatus(
    bool Geinstalleerd,
    bool Draait,
    string ConfigMap,
    string Provider,
    string Model,
    IReadOnlyList<HermesLokaalModel> LokaleModellen,
    string? Melding = null,
    int MaxContext = HermesStatus.StandaardMaxContext,
    int GeladenContext = 0)
{
    public static GooseStatus Leeg { get; } = new(false, false, string.Empty, string.Empty, string.Empty, []);
}

/// <summary>
/// Wat Jabasoft in Goose wijzigt: het model van de Jabasoft-provider (die wordt dan ook de actieve), en tot
/// hoever de context van dat model mag meegroeien (0 = laten zoals het staat). Die grens gaat als context_limit
/// in Goose' jabasoft.json - Goose gaat daar samenvatten, de broker groeit tot daar mee (zie HermesProxy).
/// </summary>
public sealed record GooseInstelling(string Model, int MaxContext = 0);

public sealed record GooseResult(bool Success, GooseStatus Status, string? ErrorMessage);

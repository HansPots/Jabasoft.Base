namespace Jabasoft.Base.AiBroker;

/// <summary>
/// De onderdelen waarvoor je in de modeltabel (Jabasoft, Instellingen) per
/// model een ster kunt zetten: een eigen favoriet (geel) of onze samen
/// vastgestelde keuze (groen). Deze namen zijn de sleutels in
/// <see cref="AiServerSettings.FavorietenPerOnderdeel"/> - niet hernoemen,
/// dan raken de bewaarde favorieten kwijt.
/// </summary>
public static class Onderdelen
{
    public const string Goose = "Goose";
    public const string Hermes = "Hermes";
    public const string LocalAiStudio = "LocalAiStudio";
    public const string ImageFunn = "ImageFunn";
    public const string TabStudio = "TabStudio";

    /// <summary>Alle onderdelen, in de volgorde van de kolommen in de modeltabel.</summary>
    public static readonly IReadOnlyList<string> Alle = [Goose, Hermes, LocalAiStudio, ImageFunn, TabStudio];
}

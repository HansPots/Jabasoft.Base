using System.Text.RegularExpressions;

namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Het opschonen van wat een model terugstuurt, vóór het op het scherm
/// komt. Staat hier (niet in een losse app) omdat elk scherm van elke
/// JabaSoft-app dat met de broker praat hetzelfde antwoord te zien moet
/// krijgen.
/// </summary>
public static partial class AiAntwoord
{
    /// <summary>
    /// Haalt de denkblokken eruit. Modellen als Qwen3 denken hardop: ze
    /// zetten hun overweging tussen &lt;think&gt; en &lt;/think&gt; en pas
    /// daarna komt het eigenlijke antwoord. Dat hoort niet op het scherm -
    /// je stelde een vraag, geen kladblok.
    ///
    /// Sommige servers knippen de OPENINGSTAG er zelf al af en laten
    /// alleen de sluittag staan. Dan is alles tot en met die sluittag nog
    /// steeds het gedachtespoor, dus dat gaat er ook af.
    ///
    /// Blijft er na het opschonen niets over - het model heeft alleen maar
    /// gedacht - dan geven we terug wat er stond. Beter een rommelig
    /// antwoord dan een leeg vak.
    /// </summary>
    public static string ZonderDenkblokken(string? antwoord)
    {
        if (string.IsNullOrWhiteSpace(antwoord))
        {
            return string.Empty;
        }

        var schoon = HeleBlokken().Replace(antwoord, string.Empty);

        // Alleen als de opening echt ontbreekt: anders zou dit bij een
        // antwoord dat zelf over </think> gaat het halve antwoord weghalen.
        if (!schoon.Contains("<think", StringComparison.OrdinalIgnoreCase))
        {
            schoon = LosseSluittag().Replace(schoon, string.Empty);
        }

        schoon = schoon.Trim();

        return schoon.Length == 0 ? antwoord.Trim() : schoon;
    }

    /// <summary>
    /// Hetzelfde, maar voor tekst die nog binnenkomt. Een denkblok dat nog
    /// niet is afgesloten hoort ook niet op het scherm: alles vanaf een
    /// openstaande &lt;think&gt; blijft weg tot de sluittag binnen is.
    ///
    /// Zonder dit zou je het model eerst zijn hele overweging zien
    /// uittypen, om die daarna te zien verdwijnen zodra het blok sluit.
    /// </summary>
    public static string TijdensStromen(string? ruw)
    {
        if (string.IsNullOrEmpty(ruw))
        {
            return string.Empty;
        }

        var schoon = HeleBlokken().Replace(ruw, string.Empty);

        var open = schoon.LastIndexOf("<think", StringComparison.OrdinalIgnoreCase);

        if (open >= 0)
        {
            schoon = schoon[..open];
        }

        return schoon.TrimStart();
    }

    /// <summary>
    /// Het OMGEKEERDE van <see cref="ZonderDenkblokken"/>: alleen het
    /// gedachtespoor, niet het antwoord. Gebruikt door Logregel, die het
    /// denken en het antwoord apart wil laten zien in plaats van als één
    /// blok. Leeg als er geen (afgesloten) denkblok in zit - dan heeft dit
    /// model niet hardop gedacht, en is er niets om te tonen.
    /// </summary>
    public static string Denkblok(string? ruw)
    {
        if (string.IsNullOrWhiteSpace(ruw))
        {
            return string.Empty;
        }

        var match = DenkblokInhoud().Match(ruw);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    [GeneratedRegex(@"<think>.*?</think>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex HeleBlokken();

    [GeneratedRegex(@"^.*?</think>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex LosseSluittag();

    [GeneratedRegex(@"<think>(.*?)</think>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex DenkblokInhoud();
}

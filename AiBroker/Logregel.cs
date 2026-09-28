using System.ComponentModel;

namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Eén bericht zoals het naar de broker ging - los, zodat een logscherm het
/// per bericht kan tonen in plaats van als één muur tekst.
///
/// <paramref name="Images"/> zijn de bijgevoegde afbeeldingen (base64-PNG,
/// zelfde vorm als <see cref="ChatMessage.Images"/>) - een logscherm mag ze
/// tonen (als thumbnail, aan te klikken), maar hoeft dat niet: <see cref="Inhoud"/>
/// heeft altijd al een tekstnotitie met het aantal, voor een scherm dat
/// geen afbeeldingen rendert.
/// </summary>
public sealed record LogBericht(string Rol, string Inhoud, IReadOnlyList<string>? Images = null)
{
    /// <summary>Waar Analyse-berichten (zie ZoekContextAsync in LocalAiStudio.App) mee beginnen - herkend om alleen de bestandsnaam te tonen, niet de hele inhoud.</summary>
    private const string BestandVoorvoegsel = "Bestand: ";

    /// <summary>Of er iets te tonen valt in een thumbnail-rij.</summary>
    public bool HeeftAfbeeldingen => Images is { Count: > 0 };

    /// <summary>
    /// Het opschrift van dit ene bericht. Is dit een bestand (zie
    /// ZoekContextAsync), dan is dat de bestandsnaam; anders de rol met
    /// een lengte, zodat een lang systeembericht of gespreksverloop niet in
    /// één keer het beeld vult.
    /// </summary>
    public string Opschrift
    {
        get
        {
            if (Inhoud.StartsWith(BestandVoorvoegsel, StringComparison.Ordinal))
            {
                var eersteRegelEinde = Inhoud.IndexOf('\n');
                return eersteRegelEinde < 0 ? Inhoud : Inhoud[..eersteRegelEinde];
            }

            return $"[{Rol}] {Inhoud.Length} tekens";
        }
    }
}

/// <summary>
/// Eén logregel tussen een vraag en het antwoord: wat er precies naar het
/// model ging, en wat erop terugkwam - ongefilterd, ook het gedachtespoor
/// dat een scherm normaal wegfiltert.
///
/// Wordt door <see cref="AiBrokerClient"/> zelf gemaakt en bijgehouden bij
/// elke <see cref="IAiBrokerClient.ChatAsync"/>/<see cref="IAiBrokerClient.ChatStreamAsync"/>-aanroep
/// waar een <c>log</c>-collectie is meegegeven - een app hoeft dus zelf
/// geen Logregel meer te bouwen of bij te werken, alleen zijn eigen
/// ObservableCollection mee te geven en te tonen (zie LocalAiStudio.App's
/// LogPaneel/IHeeftLog).
/// </summary>
public sealed class Logregel : INotifyPropertyChanged
{
    private string _ontvangenRuw = string.Empty;

    /// <summary>
    /// <paramref name="soort"/> is bijvoorbeeld "Daily chat" of
    /// "Codevoorstel — Foo.cs" (meestal gewoon <see cref="ChatRequest.Onderdeel"/>)
    /// - staat voorop in Samenvatting, zodat een gebruikersronde en
    /// bijvoorbeeld een kruiscontrole uit elkaar te houden zijn.
    ///
    /// Een bericht met bijgevoegde afbeeldingen (<see cref="ChatMessage.Images"/>)
    /// krijgt er automatisch een notitie met het aantal bij - de
    /// afbeeldingen zelf horen niet in een tekstlog.
    /// </summary>
    public Logregel(IReadOnlyList<ChatMessage> verzonden, string soort)
    {
        VerzondenBerichten = verzonden
            .Select(bericht => new LogBericht(
                bericht.Role,
                bericht.Images is { Count: > 0 }
                    ? $"{bericht.Content}\n\n[{bericht.Images.Count} afbeelding(en) meegestuurd]"
                    : bericht.Content,
                bericht.Images))
            .ToList();
        Soort = soort;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Soort { get; }

    public IReadOnlyList<LogBericht> VerzondenBerichten { get; }

    /// <summary>Groeit terwijl het antwoord binnenstroomt - vandaar INotifyPropertyChanged, net als Gespreksregel.Tekst in LocalAiStudio.App.</summary>
    public string OntvangenRuw
    {
        get => _ontvangenRuw;
        set
        {
            if (_ontvangenRuw == value)
            {
                return;
            }

            _ontvangenRuw = value;

            foreach (var naam in (string[])[nameof(OntvangenRuw), nameof(Denken), nameof(HeeftDenken), nameof(Antwoord), nameof(Samenvatting), nameof(OntvangenSamenvatting)])
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(naam));
            }
        }
    }

    /// <summary>Het gedachtespoor apart - leeg als dit model niet hardop denkt (of nog niet klaar is met denken).</summary>
    public string Denken => AiAntwoord.Denkblok(_ontvangenRuw);

    public bool HeeftDenken => Denken.Length > 0;

    /// <summary>Het antwoord zonder het gedachtespoor - wat er ook echt op het scherm staat.</summary>
    public string Antwoord => AiAntwoord.ZonderDenkblokken(_ontvangenRuw);

    private int VerzondenTotaal => VerzondenBerichten.Sum(bericht => bericht.Inhoud.Length);

    /// <summary>Eén regel samenvatting: het soort voorop, dan het totaal.</summary>
    public string Samenvatting =>
        $"{Soort} — {VerzondenBerichten.Count} berichten verzonden ({VerzondenTotaal} tekens), {_ontvangenRuw.Length} tekens terug";

    public string VerzondenSamenvatting => $"— {VerzondenBerichten.Count} berichten, {VerzondenTotaal} tekens";

    public string OntvangenSamenvatting => HeeftDenken
        ? $"— {_ontvangenRuw.Length} tekens, waarvan {Denken.Length} denken"
        : $"— {_ontvangenRuw.Length} tekens";
}

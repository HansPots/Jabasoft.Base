using System.Text.Json.Serialization;

namespace Jabasoft.Base.AiBroker;

/// <summary>
/// One turn in a chat conversation. Role is "system", "user", or "assistant".
/// <see cref="Images"/> zijn optionele afbeeldingen bij dit bericht, als
/// base64 (zonder "data:"-voorvoegsel) van een PNG/JPEG - alleen zinvol voor
/// een model dat beelden kan lezen (een "vision"-model).
/// </summary>
public sealed record ChatMessage(string Role, string Content, IReadOnlyList<string>? Images = null);

/// <summary>
/// A chat request sent to Jabasoft.Broker. <see cref="Application"/> is the
/// calling app's own name (e.g. "Stylebook", "TabStudio") - identifies which
/// app made the call, for whenever the broker attributes usage again.
/// <see cref="Onderdeel"/> is het onderdeel BINNEN die app dat de aanroep
/// deed (bijvoorbeeld "Daily chat", "Codevoorstel", "Afbeelding"), zodat het
/// tokenoverzicht kan laten zien waar het verbruik heen gaat. Optioneel.
/// </summary>
public sealed record ChatRequest(
    AiProvider Provider,
    string ServerUrl,
    string Model,
    IReadOnlyList<ChatMessage> Messages,
    string Application,
    double? Temperature = null,
    string? Onderdeel = null);

/// <summary>
/// The outcome of a chat request. Zowel Ollama als LM Studio geven vraag en
/// antwoord apart op. Geeft een server alleen een TOTAAL, dan gaat dat
/// volledig naar <see cref="PromptTokens"/> en blijft
/// <see cref="CompletionTokens"/> op 0 - beter alles aan een kant dan een
/// verzonnen verdeling.
/// </summary>
public sealed record ChatResult(bool Success, string Reply, string? ErrorMessage, long PromptTokens = 0, long CompletionTokens = 0);

/// <summary>An embedding request sent to Jabasoft.Broker.</summary>
public sealed record EmbedRequest(AiProvider Provider, string ServerUrl, string Model, string Text, string Application, string? Onderdeel = null);

/// <summary>The outcome of an embedding request.</summary>
public sealed record EmbedResult(bool Success, float[] Vector, string? ErrorMessage, long TokensUsed = 0);

/// <summary>A model-listing/reachability result - the broker caches this per (provider, serverUrl) for a short TTL.</summary>
public sealed record ModelListResult(bool Success, IReadOnlyList<string> Models, string? ErrorMessage);

/// <summary>A connection-test request sent to Jabasoft.Broker.</summary>
public sealed record TestConnectionRequest(AiProvider Provider, string ServerUrl, string Model);

/// <summary>The outcome of a connection test.</summary>
public sealed record ConnectionTestResult(bool Success, string Message);

/// <summary>
/// Wat er van EEN soort AI-server onthouden wordt: waar hij draait en
/// welke drie modellen je er gebruikt.
///
/// Per soort apart, want de namen komen niet overeen: hetzelfde model heet
/// in LM Studio "qwen/qwen2.5-coder-14b" en in Ollama "qwen2.5-coder:14b".
/// Zou er een gedeelde modelnaam staan, dan klopt hij per definitie voor
/// een van de twee niet en staat de gezondheidspil op rood zodra je wisselt.
///
/// <see cref="CodeModel"/> is bewust apart van <see cref="ChatModel"/>: een
/// codevoorstel (LocalAiStudio.App, Controls/Projectassistent) herschrijft
/// een heel bestand en verwacht dat er verder NIETS aan verandert - een
/// model dat daarvoor getraind is (bv. een Coder-variant) houdt zich daar
/// beter aan dan een algemeen chatmodel. Staat hij leeg, dan valt
/// LocalAiStudio terug op ChatModel - deze instelling is dus optioneel.
///
/// <see cref="ControleModel"/> hoort bij <see cref="CodeModel"/>: waar
/// CodeModel het antwoord GEEFT, checkt ControleModel dat antwoord
/// achteraf tegen dezelfde broncode - een ANDER model laten narekenen wat
/// het eerste beweerde, in plaats van een model zijn eigen werk laten
/// nakijken. Staat hij leeg, dan gebeurt er GEEN controle (bewust geen
/// terugval op CodeModel/ChatModel: dat zou het antwoord alsnog door
/// zichzelf laten nakijken) - deze instelling is optioneel.
///
/// <see cref="BeeldModel"/> is het model dat een bijgevoegde afbeelding
/// leest en beschrijft (een model met beeldherkenning, zoals gemma3). Die
/// beschrijving gaat als tekst naar het gewone model - zo hoeft het
/// codemodel zelf geen plaatjes te kunnen. Leeg = geen afbeeldingen
/// mogelijk; deze instelling is dus optioneel.
///
/// <see cref="Sterren"/> en <see cref="MaxDenktijdSeconden"/> zijn per
/// MODELNAAM (niet alleen de vier hierboven, elk model in de keuzelijst kan
/// erin staan): een handmatige kwaliteitsbeoordeling (0-5, overschrijft de
/// automatische schatting op grootte - zie Jabasoft.App.Controls.Modelkeuze)
/// en hoe lang een model hardop mag nadenken voordat de broker het antwoord
/// afbreekt (0 of ontbrekend = geen apart limiet, de gewone
/// <see cref="AiSettings.ChatTimeoutSeconden"/> blijft dan gelden). Beide
/// optioneel en leeg bij een verse installatie.
///
/// <see cref="Omschrijvingen"/> is er ook per modelnaam: een korte, zelf
/// getypte notitie over waar dat model goed in is (bijvoorbeeld "traag maar
/// grondig" of "goed in XAML"). Puur ter herinnering voor de gebruiker - de
/// applicaties doen er niets mee. Optioneel.
/// </summary>
public sealed record AiServerSettings(
    string Url,
    string ChatModel,
    string EmbedModel,
    string CodeModel = "",
    string ControleModel = "",
    string BeeldModel = "",
    IReadOnlyDictionary<string, int>? Sterren = null,
    IReadOnlyDictionary<string, int>? MaxDenktijdSeconden = null,
    IReadOnlyDictionary<string, string>? Omschrijvingen = null,
    IReadOnlyList<string>? Favorieten = null,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? FavorietenPerOnderdeel = null,
    IReadOnlyList<string>? Verborgen = null,
    // De modellen van Goose en Hermes OP DEZE SERVER. Voor LM Studio staan ze
    // in de eigen config van Goose en Hermes (die bestanden blijven de
    // LM Studio-namen houden); voor Ollama hier, omdat de namen daar anders
    // zijn. Zo neemt wisselen van chatserver ook het model van Goose en Hermes
    // mee. Leeg = het chatmodel van deze server. HermesLokaalModel is het
    // lokale bijtaak-/beeldmodel van de Hermes-kaart.
    string GooseModel = "",
    string HermesModel = "",
    string HermesLokaalModel = "")
{
    /// <summary>
    /// Heeft de gebruiker dit model in de modeltabel op "niet tonen" gezet? Dan staat het niet meer in de
    /// modelkeuzelijsten (AI-, Hermes- en Goose-kaart, en de modellijst die Hermes en Goose via de broker zien) -
    /// behalve waar het nu ingesteld staat, anders zou een kaart je keuze stilletjes wissen.
    /// </summary>
    public bool IsVerborgen(string model) =>
        !string.IsNullOrWhiteSpace(model) && Verborgen is not null && Verborgen.Contains(model, StringComparer.OrdinalIgnoreCase);

    /// <summary>Dezelfde instellingen, met dit model wel of niet verborgen.</summary>
    public AiServerSettings MetVerborgen(string model, bool verborgen)
    {
        var lijst = (Verborgen ?? []).Where(naam => !string.Equals(naam, model, StringComparison.OrdinalIgnoreCase)).ToList();
        if (verborgen)
        {
            lijst.Add(model);
        }

        return this with { Verborgen = lijst };
    }

    /// <summary>
    /// Is dit model een favoriet voor dit onderdeel (gele ster in die kolom van de modeltabel)? Per onderdeel
    /// kunnen het er meer zijn. <see cref="Favorieten"/> is de oude lijst van vóór de splitsing per onderdeel -
    /// de broker zet die bij het laden om naar alle onderdelen (zie AiSettingsStore).
    /// </summary>
    public bool IsFavoriet(string model, string onderdeel) =>
        !string.IsNullOrWhiteSpace(model) &&
        FavorietenPerOnderdeel is not null &&
        FavorietenPerOnderdeel.TryGetValue(onderdeel, out var lijst) &&
        lijst.Contains(model, StringComparer.OrdinalIgnoreCase);

    /// <summary>Is dit model voor minstens één onderdeel een favoriet?</summary>
    public bool IsFavoriet(string model) =>
        !string.IsNullOrWhiteSpace(model) &&
        FavorietenPerOnderdeel is not null &&
        FavorietenPerOnderdeel.Values.Any(lijst => lijst.Contains(model, StringComparer.OrdinalIgnoreCase));

    /// <summary>Dezelfde instellingen, met dit model voor dit onderdeel wel of niet als favoriet.</summary>
    public AiServerSettings MetFavoriet(string model, string onderdeel, bool favoriet)
    {
        var alle = new Dictionary<string, IReadOnlyList<string>>(FavorietenPerOnderdeel ?? new Dictionary<string, IReadOnlyList<string>>(), StringComparer.OrdinalIgnoreCase);
        var lijst = (alle.TryGetValue(onderdeel, out var huidig) ? huidig : []).ToList();
        lijst.RemoveAll(naam => string.Equals(naam, model, StringComparison.OrdinalIgnoreCase));
        if (favoriet)
        {
            lijst.Add(model);
        }

        alle[onderdeel] = lijst;
        return this with { FavorietenPerOnderdeel = alle };
    }

    /// <summary>De ingestelde modellen, zonder de lege. Dit is wat er aanwezig moet zijn.</summary>
    [JsonIgnore]
    public IReadOnlyList<string> Models =>
        new[] { ChatModel, EmbedModel, CodeModel, ControleModel, BeeldModel }.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>
    /// De sleutel waaronder de STANDAARD voor alle modellen staat (Jabasoft,
    /// bovenaan de AI-kaart) - geen echte modelnaam, dus botst nooit met een
    /// server die toevallig zo'n model aanbiedt.
    /// </summary>
    public const string StandaardSleutel = "*";

    /// <summary>
    /// De handmatige sterren (0-5) voor dit model: het model zelf als dat
    /// ingesteld staat, anders de standaard voor alle modellen, anders null
    /// (dan geldt de automatische schatting op grootte).
    /// </summary>
    public int? SterrenVoor(string model) => Opzoeken(Sterren, model);

    /// <summary>
    /// De ingestelde maximale denktijd (seconden) voor dit model: het model
    /// zelf, anders de standaard voor alle modellen, anders 0 (geen apart
    /// limiet - de gewone ChatTimeoutSeconden blijft dan gelden).
    /// </summary>
    public int DenktijdVoor(string model) => Opzoeken(MaxDenktijdSeconden, model) ?? 0;

    /// <summary>De zelf getypte notitie bij dit model, of leeg als er niets staat. Geen "standaard"-terugval: een omschrijving is per model persoonlijk.</summary>
    public string OmschrijvingVoor(string model) =>
        !string.IsNullOrWhiteSpace(model) && Omschrijvingen is not null && Omschrijvingen.TryGetValue(model, out var tekst) ? tekst : string.Empty;

    private static int? Opzoeken(IReadOnlyDictionary<string, int>? waarden, string model)
    {
        if (waarden is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(model) && !string.Equals(model, StandaardSleutel, StringComparison.Ordinal)
            && waarden.TryGetValue(model, out var eigen))
        {
            return eigen;
        }

        return waarden.TryGetValue(StandaardSleutel, out var standaard) ? standaard : null;
    }
}

/// <summary>
/// De AI-instelling van deze machine: welke serversoort er gebruikt wordt,
/// en van elke soort het adres en de modellen.
///
/// Er is er maar EEN, en hij staat bij de broker. Dat is met opzet: de
/// broker is de gedeelde voorziening waar alle applicaties langs gaan, dus
/// wie daar het model omzet, zet het voor de hele familie om. Een app
/// bewaart hier zelf niets van.
///
/// Beide soorten blijven naast elkaar bewaard, ook de soort die nu niet
/// gekozen is: wisselen en terugwisselen laat je instelling dus heel.
///
/// <see cref="MinimumSemanticScore"/> staat hier en niet per serversoort:
/// het zegt iets over hoe streng semantisch zoeken is, niet over een
/// server. Zie Jabasoft.Broker/SearchIndexStore voor hoe hij gebruikt
/// wordt.
/// </summary>
public sealed record AiSettings(
    AiProvider Provider,
    AiServerSettings LmStudio,
    AiServerSettings Ollama,
    // De ondergrens (0-1, cosinus-gelijkenis) waaronder een semantische
    // treffer als "onzeker" geldt in plaats van "gevonden" - zie
    // Zoeken.xaml in LocalAiStudio.App. 0,5 is een eerste schatting, geen
    // vaste waarheid: hoe de scores verdeeld liggen hangt af van het
    // embeddingmodel, dus dit is bedoeld om aan te passen op wat er in de
    // praktijk als "nog zinnig" aanvoelt.
    double MinimumSemanticScore = 0.5,
    // Hoeveel EXTRA ronden Projectassistent (LocalAiStudio.App) mag vragen
    // om meer projectcontext als het model zelf aangeeft dat de eerste
    // lading niet genoeg was. 0 zet dit uit - dan krijgt het model maar
    // één kans, zoals voorheen. Instelbaar 0-5, standaard 3 - net als
    // MinimumSemanticScore hierboven staat dit hier en niet per
    // serversoort: het gaat over gedrag, niet over een server.
    int MaxContextRondes = 3,
    // Hoe lang een app op een antwoord van een model wacht voordat de
    // verbinding wordt afgebroken - geldt voor ELK model dat via de broker
    // aangeroepen wordt (chat, code, controle), niet voor een specifieke
    // serversoort. Standaard 300s (5 min), net zoals AiBrokerClient dat al
    // altijd deed voordat dit instelbaar werd.
    double ChatTimeoutSeconden = 300,
    // Op welke serversoort het ZOEKEN (embedding) loopt - los van
    // Provider, dat de chat kiest. De zoekindex hoort bij één
    // embeddingmodel; zou hij met de chatserver meewisselen, dan moest bij
    // elke wissel LM Studio <-> Ollama de hele index opnieuw. Zo wisselt de
    // chat (en daarmee ook Goose en Hermes) vrij, en blijft de index geldig.
    // Het embeddingmodel zelf staat bij die soort (AiServerSettings.EmbedModel).
    // null = nog niet gekozen (instelling van vóór deze splitsing): de broker
    // zet hem bij het laden vast op de soort die toen gekozen was.
    AiProvider? ZoekProvider = null)
{
    /// <summary>De standaardpoort van LM Studio's OpenAI-compatibele server.</summary>
    public const string DefaultLmStudioUrl = "http://localhost:1234";

    /// <summary>De standaardpoort van Ollama.</summary>
    public const string DefaultOllamaUrl = "http://localhost:11434";

    /// <summary>Waar een verse installatie mee begint: LM Studio, nog zonder gekozen modellen.</summary>
    public static AiSettings Default { get; } = new(
        AiProvider.LmStudio,
        new AiServerSettings(DefaultLmStudioUrl, string.Empty, string.Empty),
        new AiServerSettings(DefaultOllamaUrl, string.Empty, string.Empty));

    /// <summary>De soort die NU gekozen is - waar alles wat de apps doen langs gaat.</summary>
    [JsonIgnore]
    public AiServerSettings Active => Provider == AiProvider.Ollama ? Ollama : LmStudio;

    /// <summary>Het adres van de soort die nu gekozen is.</summary>
    [JsonIgnore]
    public string ActiveServerUrl => Active.Url;

    /// <summary>De serversoort waar het zoeken (embedding en de zoekindex) langs gaat - zie <see cref="ZoekProvider"/>.</summary>
    [JsonIgnore]
    public AiProvider ZoekSoort => ZoekProvider ?? Provider;

    /// <summary>De gegevens van de zoekserver.</summary>
    [JsonIgnore]
    public AiServerSettings ZoekServer => ZoekSoort == AiProvider.Ollama ? Ollama : LmStudio;

    /// <summary>Het adres van de zoekserver.</summary>
    [JsonIgnore]
    public string ZoekServerUrl => ZoekServer.Url;

    /// <summary>Het embeddingmodel waarmee gezocht en geïndexeerd wordt. Gebruik dit, niet Active.EmbedModel.</summary>
    [JsonIgnore]
    public string ZoekModel => ZoekServer.EmbedModel;

    /// <summary>
    /// De modellen die op de nu gekozen (chat)server aanwezig moeten zijn. Het embeddingmodel alleen als het
    /// zoeken ook op deze server loopt - anders hoort het bij de andere server en hoeft het hier niet te staan.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<string> ConfiguredModels =>
        ZoekSoort == Provider ? Active.Models : (Active with { EmbedModel = string.Empty }).Models;

    /// <summary>Vervangt de gegevens van EEN soort en laat de andere staan.</summary>
    public AiSettings With(AiProvider provider, AiServerSettings server) => provider == AiProvider.Ollama
        ? this with { Ollama = server }
        : this with { LmStudio = server };
}

/// <summary>Het ophalen of opslaan van <see cref="AiSettings"/> bij de broker.</summary>
public sealed record AiSettingsResult(bool Success, AiSettings Settings, string? ErrorMessage);

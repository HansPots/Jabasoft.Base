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
/// </summary>
public sealed record ChatRequest(
    AiProvider Provider,
    string ServerUrl,
    string Model,
    IReadOnlyList<ChatMessage> Messages,
    string Application,
    double? Temperature = null);

/// <summary>
/// The outcome of a chat request. Zowel Ollama als LM Studio geven vraag en
/// antwoord apart op. Geeft een server alleen een TOTAAL, dan gaat dat
/// volledig naar <see cref="PromptTokens"/> en blijft
/// <see cref="CompletionTokens"/> op 0 - beter alles aan een kant dan een
/// verzonnen verdeling.
/// </summary>
public sealed record ChatResult(bool Success, string Reply, string? ErrorMessage, long PromptTokens = 0, long CompletionTokens = 0);

/// <summary>An embedding request sent to Jabasoft.Broker.</summary>
public sealed record EmbedRequest(AiProvider Provider, string ServerUrl, string Model, string Text, string Application);

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
/// </summary>
public sealed record AiServerSettings(string Url, string ChatModel, string EmbedModel, string CodeModel = "", string ControleModel = "", string BeeldModel = "")
{
    /// <summary>De ingestelde modellen, zonder de lege. Dit is wat er aanwezig moet zijn.</summary>
    [JsonIgnore]
    public IReadOnlyList<string> Models =>
        new[] { ChatModel, EmbedModel, CodeModel, ControleModel, BeeldModel }.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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
    double ChatTimeoutSeconden = 300)
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

    /// <summary>De modellen die op de nu gekozen server aanwezig moeten zijn.</summary>
    [JsonIgnore]
    public IReadOnlyList<string> ConfiguredModels => Active.Models;

    /// <summary>Vervangt de gegevens van EEN soort en laat de andere staan.</summary>
    public AiSettings With(AiProvider provider, AiServerSettings server) => provider == AiProvider.Ollama
        ? this with { Ollama = server }
        : this with { LmStudio = server };
}

/// <summary>Het ophalen of opslaan van <see cref="AiSettings"/> bij de broker.</summary>
public sealed record AiSettingsResult(bool Success, AiSettings Settings, string? ErrorMessage);

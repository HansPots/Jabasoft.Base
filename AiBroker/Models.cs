using System.Text.Json.Serialization;

namespace Jabasoft.Base.AiBroker;

/// <summary>One turn in a chat conversation. Role is "system", "user", or "assistant".</summary>
public sealed record ChatMessage(string Role, string Content);

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
/// welke twee modellen je er gebruikt.
///
/// Per soort apart, want de namen komen niet overeen: hetzelfde model heet
/// in LM Studio "qwen/qwen2.5-coder-14b" en in Ollama "qwen2.5-coder:14b".
/// Zou er een gedeelde modelnaam staan, dan klopt hij per definitie voor
/// een van de twee niet en staat de gezondheidspil op rood zodra je wisselt.
/// </summary>
public sealed record AiServerSettings(string Url, string ChatModel, string EmbedModel)
{
    /// <summary>De ingestelde modellen, zonder de lege. Dit is wat er aanwezig moet zijn.</summary>
    [JsonIgnore]
    public IReadOnlyList<string> Models =>
        new[] { ChatModel, EmbedModel }.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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
/// </summary>
public sealed record AiSettings(
    AiProvider Provider,
    AiServerSettings LmStudio,
    AiServerSettings Ollama)
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

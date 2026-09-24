namespace Jabasoft.Base.AiBroker;

/// <summary>Eén bestand dat geïndexeerd wordt: het pad en de tekst zoals hij nu op schijf staat.</summary>
public sealed record SearchIndexFile(string Path, string Content);

/// <summary>
/// Een verzoek om (een deel van) een project te (her)indexeren voor
/// semantisch zoeken. Provider/ServerUrl/Model wijzen naar het EMBEDDING-
/// model - dezelfde manier waarop <see cref="ChatRequest"/> en
/// <see cref="EmbedRequest"/> die al expliciet meegeven in plaats van dat
/// de broker zelf de ingestelde modellen erbij zoekt.
/// </summary>
public sealed record SearchIndexRequest(
    AiProvider Provider,
    string ServerUrl,
    string Model,
    string Project,
    IReadOnlyList<SearchIndexFile> Files,
    string Application);

/// <summary>Hoeveel bestanden bijgewerkt zijn, en hoeveel overgeslagen (leeg, of het embedden mislukte).</summary>
public sealed record SearchIndexResult(bool Success, int Indexed, int Skipped, string? ErrorMessage);

/// <summary>
/// Een semantische zoekvraag: dezelfde vraag naar een vector, vergeleken
/// met wat er voor dit project geïndexeerd staat.
///
/// <see cref="MaxResults"/> is niet zomaar een afkapgrens: haalt de vraag
/// er MEER dan dit aantal treffers uit die de ondergrens van de broker
/// halen (zie <see cref="SemanticSearchHit.MeetsThreshold"/>), dan komen
/// die ALLEMAAL terug - een relevant antwoord afkappen om een rond getal
/// is erger dan een iets langere lijst. Haalt de vraag er minder op, dan
/// wordt er tot dit aantal aangevuld met de beste van de rest, ook al
/// halen die de grens niet - beter een onzeker antwoord dan een leeg
/// scherm.
/// </summary>
public sealed record SemanticSearchRequest(
    AiProvider Provider,
    string ServerUrl,
    string Model,
    string Project,
    string Query,
    string Application,
    int MaxResults = 5);

/// <summary>
/// Eén treffer: het bestand, een fragment van de geïndexeerde tekst, hoe
/// dicht het bij de vraag ligt (1 = identiek, 0 = niets mee te maken), en
/// of dat de ondergrens haalt die de broker aanhoudt
/// (SearchIndexStore.MinimumGelijkenis) - de aanroeper hoeft die grens dan
/// zelf niet te kennen om te weten of een treffer nog te vertrouwen is.
/// </summary>
public sealed record SemanticSearchHit(string FilePath, string Snippet, double Score, bool MeetsThreshold);

public sealed record SemanticSearchResult(bool Success, IReadOnlyList<SemanticSearchHit> Hits, string? ErrorMessage);

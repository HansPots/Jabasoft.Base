namespace Jabasoft.Ai.Data.Entities;

/// <summary>
/// Eén geïndexeerd bestand van één project: de tekst zoals hij op het
/// moment van indexeren was, en het embedding-vector dat daarbij hoort.
/// Dit is waar semantisch zoeken (Zoeken-scherm van LocalAiStudio) tegen
/// zoekt - keyword search gaat rechtstreeks over de bestanden op schijf en
/// heeft deze tabel niet nodig.
///
/// Eén rij per (Project, FilePath) - opnieuw indexeren is een upsert, geen
/// nieuwe rij. Project is het .sln/.slnx/.csproj-pad zoals
/// LocalAiStudio.App.Analyse.HuidigProject.Pad het kent, dus twee
/// verschillende projecten botsen nooit met elkaars bestanden.
///
/// Embedding staat bewust als tekst (JSON van een getallenrij) en niet als
/// het native VECTOR-type van SQL Server 2025: dit is de EERSTE, simpele
/// opzet - de vergelijking gebeurt nu in C# (zie SearchIndexStore in
/// Jabasoft.Broker). Wordt dit te traag bij grote projecten, dan is de
/// volgende stap een VECTOR-kolom met een DiskANN-index en
/// VECTOR_DISTANCE in T-SQL; dat is een migratie op deze tabel, geen
/// nieuwe tabel.
/// </summary>
public class SearchDocument : IAuditableEntity
{
    public int Id { get; set; }

    /// <summary>Het projectbestand (.sln/.slnx/.csproj) waar dit bestand bij hoort.</summary>
    public string Project { get; set; } = string.Empty;

    /// <summary>Het volledige pad naar het geïndexeerde bestand op schijf.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Welk embedding-model dit vector geproduceerd heeft - wissel je van model, dan zijn oude vectors niet meer vergelijkbaar met nieuwe.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>De tekst die geëmbed is (eventueel ingekort) - voor het tonen van een fragment bij een treffer, zonder het bestand opnieuw te hoeven lezen.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Het embedding-vector, als JSON-array van getallen.</summary>
    public string Embedding { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

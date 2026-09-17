using System.Text.Json;

namespace Jabasoft.Base.Layout;

/// <summary>
/// Bewaart schermindelingen die de gebruiker zelf versleept heeft - nu de
/// kolombreedtes achter de splitters in de header en de footer.
///
/// Slaat op in %LOCALAPPDATA%\Jabasoft\&lt;app&gt;\layout.json: dat overleeft
/// een herbouw of een nieuwe publicatie, en staat per gebruiker apart. Niet
/// naast de exe dus, want die map wordt bij elke schone build leeggehaald.
///
/// Alleen getallen, geen WPF-typen: deze bibliotheek weet niets van
/// schermen. De aanroeper rekent ze om naar GridLength en terug.
///
/// Gaat lezen of schrijven mis (geen rechten, kapot bestand), dan gebeurt
/// er niets bijzonders: de app valt gewoon terug op zijn standaardindeling.
/// Een schermindeling is het niet waard om een applicatie voor te laten
/// vastlopen.
/// </summary>
public sealed class LayoutStore
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    private readonly string _path;
    private Dictionary<string, double[]> _entries;

    public LayoutStore(string appName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appName);

        _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Jabasoft",
            appName,
            "layout.json");

        _entries = Read();
    }

    /// <summary>De bewaarde waarden onder deze sleutel, of null als er nog niets bewaard is.</summary>
    public double[]? Load(string key) => _entries.TryGetValue(key, out var values) ? values : null;

    /// <summary>Bewaart de waarden onder deze sleutel en schrijft het bestand meteen weg.</summary>
    public void Save(string key, IReadOnlyList<double> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(values);

        _entries[key] = [.. values];

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(_entries, WriteOptions));
        }
        catch (Exception)
        {
            // Zie de toelichting hierboven: niet kunnen bewaren mag niets breken.
        }
    }

    /// <summary>Vergeet wat er onder deze sleutel bewaard is, zodat de standaardindeling weer geldt.</summary>
    public void Forget(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!_entries.Remove(key))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(_entries, WriteOptions));
        }
        catch (Exception)
        {
            // Zie de toelichting hierboven.
        }
    }

    private Dictionary<string, double[]> Read()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<Dictionary<string, double[]>>(File.ReadAllText(_path)) ?? []
                : [];
        }
        catch (Exception)
        {
            return [];
        }
    }
}

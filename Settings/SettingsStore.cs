using System.Globalization;
using System.Text.Json;

namespace Jabasoft.Base.Settings;

/// <summary>
/// Bewaart de voorkeuren van een applicatie - thema, lettertype, marges,
/// venstermaat - naast de schermindeling van LayoutStore.
///
/// Waarom een BESTAND en geen database: dit is een voorkeur van deze
/// gebruiker op deze werkplek, geen gedeelde waarheid. Bovendien is het
/// nodig voordat er iets op het scherm staat; een bestand lezen kost geen
/// tijd en werkt ook als er verder niets draait, terwijl een database
/// (rechtstreeks of via de broker) eerst wakker moet worden - en dan zie
/// je het venster eerst in het verkeerde thema opkomen.
///
/// Slaat op in %LOCALAPPDATA%\Jabasoft\&lt;app&gt;\settings.json, dezelfde
/// plek als layout.json: dat overleeft een herbouw en staat per gebruiker
/// apart.
///
/// Alles wordt als TEKST bewaard, met omrekenaars eromheen. Zo blijft het
/// bestand met de hand te lezen en kan er een sleutel bij zonder dat er
/// een type omgezet hoeft te worden.
///
/// Gaat lezen of schrijven mis (geen rechten, kapot bestand), dan gebeurt
/// er niets bijzonders: de applicatie valt terug op zijn standaardwaarden.
/// Een voorkeur is het niet waard om een applicatie voor te laten
/// vastlopen.
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    private readonly string _path;
    private readonly Dictionary<string, string> _entries;

    /// <param name="appName">De mapnaam onder %LOCALAPPDATA%\Jabasoft, meestal de naam van de applicatie.</param>
    /// <param name="directory">
    /// Een andere map dan de standaard. Alleen bedoeld om dit te kunnen
    /// nakijken zonder aan de echte voorkeuren van de gebruiker te komen.
    /// </param>
    public SettingsStore(string appName, string? directory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appName);

        _path = Path.Combine(
            directory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Jabasoft",
                appName),
            "settings.json");

        _entries = Read();
    }

    /// <summary>De bewaarde tekst, of <paramref name="fallback"/> als er niets staat.</summary>
    public string GetString(string key, string fallback) =>
        _entries.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    /// <summary>Het bewaarde getal, of <paramref name="fallback"/> als er niets staat of het onleesbaar is.</summary>
    public double GetDouble(string key, double fallback) =>
        _entries.TryGetValue(key, out var value)
        && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var getal)
            ? getal
            : fallback;

    /// <summary>Het bewaarde ja/nee, of <paramref name="fallback"/> als er niets staat.</summary>
    public bool GetBool(string key, bool fallback) =>
        _entries.TryGetValue(key, out var value) && bool.TryParse(value, out var vlag) ? vlag : fallback;

    /// <summary>Staat er iets onder deze sleutel?</summary>
    public bool Has(string key) => _entries.ContainsKey(key);

    public void Set(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _entries[key] = value;
        Write([key]);
    }

    // InvariantCulture, altijd: anders staat er op een Nederlandse machine
    // een komma in het bestand en leest een andere machine er iets anders
    // in terug.
    public void Set(string key, double value) =>
        Set(key, value.ToString("R", CultureInfo.InvariantCulture));

    public void Set(string key, bool value) =>
        Set(key, value ? "true" : "false");

    /// <summary>Zet meerdere sleutels in één keer en schrijft daarna één keer weg.</summary>
    public void SetMany(IEnumerable<KeyValuePair<string, string>> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var sleutels = new List<string>();

        foreach (var (key, value) in values)
        {
            _entries[key] = value;
            sleutels.Add(key);
        }

        Write(sleutels);
    }

    /// <summary>
    /// Schrijft weg, maar bovenop wat er OP DIT MOMENT in het bestand staat.
    ///
    /// Waarom niet gewoon het eigen geheugenbeeld: dat is een foto van het
    /// moment waarop deze applicatie startte. Draait er een tweede exemplaar,
    /// of straks een tweede applicatie die dezelfde voorkeuren deelt, dan zou
    /// die bij zijn eerstvolgende opslag - al is het maar de venstermaat bij
    /// het afsluiten - zijn verouderde kopie van ALLE sleutels eroverheen
    /// gooien. Je zou dan zien dat een instelling die je net gewijzigd hebt
    /// vanzelf terugspringt.
    ///
    /// Door eerst opnieuw te lezen blijft alleen wat HIER veranderd is van
    /// deze applicatie, en de rest van wie er ook bij was.
    /// </summary>
    private void Write(IEnumerable<string> gewijzigd)
    {
        try
        {
            var opSchijf = Read();

            foreach (var sleutel in gewijzigd)
            {
                if (_entries.TryGetValue(sleutel, out var waarde))
                {
                    opSchijf[sleutel] = waarde;
                }
            }

            // Wat er intussen door een ander bij gezet is, nemen we over -
            // anders loopt dit exemplaar de rest van zijn leven achter.
            foreach (var (sleutel, waarde) in opSchijf)
            {
                _entries[sleutel] = waarde;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(opSchijf, WriteOptions));
        }
        catch (Exception)
        {
            // Zie de toelichting hierboven: niet kunnen bewaren mag niets breken.
        }
    }

    private Dictionary<string, string> Read()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return [];
            }

            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_path)) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }
}

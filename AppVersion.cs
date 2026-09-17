using System.Reflection;

namespace Jabasoft.Base;

/// <summary>
/// Het versienummer van de draaiende applicatie, zoals het in haar csproj
/// staat (&lt;Version&gt;).
///
/// Waarom uitlezen en niet ergens overtypen: een nummer dat met de hand in
/// een scherm staat kan uit de pas lopen met wat er werkelijk gebouwd is -
/// je past het aan en vergeet te bouwen, of andersom. Wat hier uitkomt is
/// per definitie het nummer van de code die NU draait, en het staat ook in
/// de eigenschappen van het exe-bestand en in een foutmelding.
/// </summary>
public static class AppVersion
{
    /// <summary>
    /// Het nummer, bijvoorbeeld "0.1.0.82". Leeg kan niet: lukt uitlezen
    /// niet, dan komt er "0.0.0.0" uit - te zien en dus te herstellen.
    ///
    /// De informationele versie heeft de voorkeur: die draagt precies wat er
    /// in het csproj staat. Sommige bouwomgevingen plakken daar nog een
    /// git-verwijzing achter met een +; alles vanaf die + gaat eraf.
    /// </summary>
    public static string Current => Of(Assembly.GetEntryAssembly());

    public static string Of(Assembly? assembly)
    {
        if (assembly is null)
        {
            return "0.0.0.0";
        }

        var informeel = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informeel))
        {
            var plus = informeel.IndexOf('+', StringComparison.Ordinal);
            return plus < 0 ? informeel : informeel[..plus];
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0.0";
    }
}

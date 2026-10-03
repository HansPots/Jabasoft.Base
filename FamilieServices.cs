using System.Diagnostics;

namespace Jabasoft.Base;

/// <summary>
/// De achtergronddiensten van de familie (de AI-broker, de beeldservice van ImageFunn, de audioservice van
/// TabStudio) opruimen die van een eerdere sessie zijn blijven staan.
///
/// Zo'n dienst leeft langer dan de app die hem startte. Dat geeft twee problemen: de broker houdt bestanden
/// van de gedeelde bibliotheek vast, waardoor bouwen mislukt, en een dienst met oude code blijft gewoon in
/// gebruik, omdat een app een draaiende dienst als "gezond" ziet en hem niet opnieuw start.
///
/// Opruimen gebeurt ALLEEN als er geen andere app van de familie draait: die kan zo'n dienst nodig hebben,
/// en daar zou je dan onder wegtrekken.
/// </summary>
public static class FamilieServices
{
    /// <summary>De poorten waarop de diensten luisteren, met hun naam voor het logboek.</summary>
    public static readonly IReadOnlyList<(string Naam, int Poort)> Diensten =
    [
        ("AI-broker", 5310),
        ("TabStudio-audioservice", 5320),
        ("ImageFunn-beeldservice", 5330),
    ];

    /// <summary>De procesnaam van de broker (zonder .exe), die ook zonder luisterende poort kan blijven hangen.</summary>
    private const string BrokerProces = "Jabasoft.Broker";

    /// <summary>Draait er een app van de familie, behalve de aanroeper zelf?</summary>
    public static bool AndereAppDraait()
    {
        var eigen = Environment.ProcessId;
        foreach (var proces in Process.GetProcesses())
        {
            using (proces)
            {
                if (proces.Id != eigen && JabasoftApps.IsFamily(proces.ProcessName))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Stopt alle diensten van de familie als er geen andere app draait. Geeft de namen terug van wat er
    /// gestopt is (leeg als er niets was, of als er een andere app draait).
    /// </summary>
    public static IReadOnlyList<string> StopOudeDiensten()
    {
        var gestopt = new List<string>();

        if (AndereAppDraait())
        {
            return gestopt;
        }

        foreach (var (naam, poort) in Diensten)
        {
            if (StopPoort(poort))
            {
                gestopt.Add(naam);
            }
        }

        // Een broker die niet meer luistert maar nog bestanden vasthoudt.
        foreach (var proces in Process.GetProcessesByName(BrokerProces))
        {
            using (proces)
            {
                if (Stop(proces))
                {
                    gestopt.Add($"{BrokerProces} (proces {proces.Id})");
                }
            }
        }

        return gestopt;
    }

    /// <summary>
    /// Stopt het proces dat op deze poort luistert (met zijn onderliggende processen), en wacht kort tot de
    /// poort vrij is. Geeft true als er iets gestopt is.
    /// </summary>
    public static bool StopPoort(int poort)
    {
        var gestopt = false;

        foreach (var pid in LuisterendeProcessen(poort))
        {
            if (pid == Environment.ProcessId)
            {
                continue;
            }

            try
            {
                using var proces = Process.GetProcessById(pid);
                gestopt |= Stop(proces);
            }
            catch (ArgumentException)
            {
                // Al weg.
            }
        }

        // De poort komt pas vrij als het besturingssysteem hem loslaat; een nieuwe dienst die er meteen op wil
        // luisteren zou anders tegen "poort al in gebruik" aanlopen.
        for (var poging = 0; gestopt && poging < 20 && LuisterendeProcessen(poort).Count > 0; poging++)
        {
            Thread.Sleep(150);
        }

        return gestopt;
    }

    private static bool Stop(Process proces)
    {
        try
        {
            proces.Kill(entireProcessTree: true);
            proces.WaitForExit(3000);
            return true;
        }
        catch (Exception)
        {
            // Geen rechten, of al afgesloten: dan is er niets aan te doen.
            return false;
        }
    }

    /// <summary>
    /// De processen die op deze TCP-poort luisteren, uit `netstat -ano`. Het commando is bewust gekozen boven
    /// een Windows-aanroep: het werkt zonder extra rechten, en de kolommen (adres, doeladres, PID) zijn
    /// onafhankelijk van de taal van Windows.
    /// </summary>
    private static List<int> LuisterendeProcessen(int poort)
    {
        var resultaat = new List<int>();

        try
        {
            using var netstat = Process.Start(new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano -p TCP",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            });

            if (netstat is null)
            {
                return resultaat;
            }

            var uitvoer = netstat.StandardOutput.ReadToEnd();
            netstat.WaitForExit(5000);

            foreach (var regel in uitvoer.Split('\n'))
            {
                var kolommen = regel.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                // Proto, lokaal adres, extern adres, status, PID. Een luisterende poort heeft als extern adres
                // 0.0.0.0:0 of [::]:0.
                if (kolommen.Length >= 5
                    && kolommen[1].EndsWith($":{poort}", StringComparison.Ordinal)
                    && (kolommen[2] == "0.0.0.0:0" || kolommen[2] == "[::]:0")
                    && int.TryParse(kolommen[^1], out var pid)
                    && !resultaat.Contains(pid))
                {
                    resultaat.Add(pid);
                }
            }
        }
        catch (Exception)
        {
            // Zonder netstat kunnen we niets opruimen; de aanroeper gaat gewoon door.
        }

        return resultaat;
    }
}

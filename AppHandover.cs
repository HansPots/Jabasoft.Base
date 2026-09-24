using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Jabasoft.Base;

/// <summary>
/// Het scherm overgeven aan een andere applicatie van de familie: die komt
/// op precies de plek te staan waar jij stond, en jij verdwijnt van het
/// scherm.
///
/// Zo voelen twee losse applicaties als één: je klikt in Jabasoft op AI
/// Studio en het lijkt of hetzelfde venster van inhoud wisselt, terwijl het
/// in werkelijkheid twee processen zijn.
///
/// Het overgeven gebeurt hier en niet door de ontvangende kant: die weet
/// niet dat er iemand op bezoek komt, en hoeft er dus ook niets voor te
/// doen. Wij kennen het vensterhandvat van de ander en zetten het gewoon
/// neer.
///
/// Belangrijk: een verborgen venster telt voor Windows niet meer als
/// hoofdvenster, dus Process.MainWindowHandle geeft daar niets voor terug.
/// Vandaar dat hier zelf alle vensters van een proces afgelopen worden,
/// zichtbaar of niet - anders zou een verborgen applicatie niet
/// teruggevonden worden en zou er een tweede exemplaar opgestart worden.
/// </summary>
public static class AppHandover
{
    /// <summary>Hoe lang we wachten tot een net gestarte applicatie een venster heeft.</summary>
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Geeft het scherm over aan de applicatie op <paramref name="executablePath"/>:
    /// zoekt of start haar, zet haar venster op dezelfde plek en grootte als
    /// <paramref name="ownWindow"/>, haalt het naar voren en verbergt daarna
    /// het onze.
    ///
    /// Lukt het vinden of starten niet, dan gebeurt er NIETS - ook het
    /// verbergen niet. Anders zou je met een leeg scherm achterblijven.
    /// </summary>
    public static bool SwitchTo(string executablePath, IntPtr ownWindow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        if (ownWindow == IntPtr.Zero)
        {
            return false;
        }

        var doel = ZoekOfStart(executablePath);
        if (doel == IntPtr.Zero)
        {
            return false;
        }

        // De plaatsing bevat de gewone rechthoek EN of het venster
        // gemaximaliseerd staat; zo neemt de ander ook die stand over.
        var plaatsing = new WindowPlacement { length = Marshal.SizeOf<WindowPlacement>() };
        if (GetWindowPlacement(ownWindow, ref plaatsing))
        {
            // Nooit als "verborgen" doorgeven - dat zou de ander meteen weer
            // van het scherm halen.
            if (plaatsing.showCmd is ShowHide or ShowMinimized)
            {
                plaatsing.showCmd = ShowNormal;
            }

            // SetWindowPlacement toont het venster al, in de stand die we
            // meegeven. Er daarna nog een ShowWindow overheen doen zou een
            // GEMAXIMALISEERD venster juist terugzetten naar zijn gewone
            // maat - dat was precies wat er misging.
            SetWindowPlacement(doel, ref plaatsing);
        }
        else
        {
            ShowWindow(doel, ShowNoActivate);
        }

        SetForegroundWindow(doel);

        ShowWindow(ownWindow, ShowHide);
        return true;
    }

    /// <summary>
    /// Haalt verborgen vensters van de familie weer tevoorschijn.
    ///
    /// Het vangnet onder <see cref="SwitchTo"/>: sluit je de applicatie die
    /// in beeld staat, dan zou de verborgen applicatie anders onzichtbaar
    /// blijven draaien - zonder taakbalkknop, dus zonder weg terug. Roep dit
    /// daarom aan bij het afsluiten.
    /// </summary>
    public static void RevealHidden()
    {
        var self = Environment.ProcessId;

        foreach (var naam in JabasoftApps.ProcessNames)
        {
            try
            {
                // Alleen het HOOFDvenster tevoorschijn halen, niet zomaar elk
                // verborgen venster van het proces: een WPF-applicatie houdt
                // er een handvol naamloze hulpvensters op na die verborgen
                // horen te blijven. Die tonen zette ze als vlekjes van 136x39
                // in de hoek van het scherm, en ze verstoorden bovendien wat
                // Windows als hoofdvenster van dat proces beschouwt.
                var venster = Hoofdvenster(naam, self);
                if (venster != IntPtr.Zero && !IsWindowVisible(venster))
                {
                    ShowWindow(venster, ShowNormal);
                }
            }
            catch (Exception)
            {
                // Een proces dat net stopte, of waar we niet bij mogen. Bij
                // het afsluiten is dat geen reden om te blijven hangen.
            }
        }
    }

    /// <summary>
    /// Zoekt een al draaiend exemplaar van DEZE applicatie (zelfde procesnaam,
    /// een ANDER proces-ID) en haalt dat naar voren. Roep dit als eerste aan
    /// bij het opstarten, vóór er een eigen venster gemaakt wordt: vindt dit
    /// iets, dan hoort de aanroeper meteen af te sluiten in plaats van door
    /// te gaan - anders staan er zo twee vensters van dezelfde app naast
    /// elkaar, allebei half opgestart.
    ///
    /// Geeft false als er niets draait (of het venster niet te vinden is);
    /// dan gaat het opstarten gewoon verder zoals altijd.
    /// </summary>
    public static bool ActivateExistingInstance()
    {
        var procesnaam = Process.GetCurrentProcess().ProcessName;
        var bestaand = Hoofdvenster(procesnaam, Environment.ProcessId);

        if (bestaand == IntPtr.Zero)
        {
            return false;
        }

        var plaatsing = new WindowPlacement { length = Marshal.SizeOf<WindowPlacement>() };
        if (GetWindowPlacement(bestaand, ref plaatsing))
        {
            if (plaatsing.showCmd is ShowHide or ShowMinimized)
            {
                plaatsing.showCmd = ShowNormal;
            }

            SetWindowPlacement(bestaand, ref plaatsing);
        }
        else
        {
            ShowWindow(bestaand, ShowNormal);
        }

        SetForegroundWindow(bestaand);
        return true;
    }

    /// <summary>Zoekt het venster van de applicatie; draait ze niet, dan wordt ze gestart en wachten we tot er een venster is.</summary>
    private static IntPtr ZoekOfStart(string executablePath)
    {
        var procesnaam = Path.GetFileNameWithoutExtension(executablePath);

        var bestaand = Hoofdvenster(procesnaam);
        if (bestaand != IntPtr.Zero)
        {
            return bestaand;
        }

        if (!File.Exists(executablePath))
        {
            return IntPtr.Zero;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = Path.GetDirectoryName(executablePath),
                UseShellExecute = true,
            });
        }
        catch (Exception)
        {
            return IntPtr.Zero;
        }

        // Wachten tot het venster er is. Zonder dit zouden we de plaatsing
        // doorgeven aan een venster dat nog niet bestaat, en zou de ander op
        // zijn eigen bewaarde plek opkomen.
        var deadline = DateTime.UtcNow.Add(StartupTimeout);
        while (DateTime.UtcNow < deadline)
        {
            var venster = Hoofdvenster(procesnaam);
            if (venster != IntPtr.Zero)
            {
                return venster;
            }

            Thread.Sleep(200);
        }

        return IntPtr.Zero;
    }

    /// <summary>De kleinste maat die we nog als hoofdvenster willen aannemen - alles daaronder is hulpwerk.</summary>
    private const int MinimaleZijde = 200;

    /// <summary>
    /// Het hoofdvenster van de applicatie, ook als het verborgen is.
    ///
    /// Waarom niet gewoon "het grootste venster": een WPF-applicatie heeft
    /// naast haar echte venster nog een handvol naamloze hulpvensters voor
    /// berichten en beeldscherminstellingen. Eentje daarvan bleek soms
    /// groter dan verwacht, en die werd dan verplaatst en zichtbaar gemaakt
    /// terwijl het echte venster bleef staan - een leeg vlak op het scherm.
    ///
    /// Het hoofdvenster herken je aan drie dingen tegelijk: het heeft een
    /// TITEL (Window.Title, ook als de titelbalk verborgen is), het heeft
    /// geen eigenaar, en het is van een redelijke maat. Blijven er dan nog
    /// meerdere over, dan wint de grootste.
    /// </summary>
    private static IntPtr Hoofdvenster(string procesnaam, int? behalveProces = null)
    {
        var beste = IntPtr.Zero;
        var besteOppervlak = 0L;

        foreach (var proces in Process.GetProcessesByName(procesnaam))
        {
            try
            {
                if (proces.Id == behalveProces)
                {
                    continue;
                }

                foreach (var venster in VenstersVan(proces.Id))
                {
                    if (!GetWindowRect(venster, out var rechthoek))
                    {
                        continue;
                    }

                    var breedte = rechthoek.Right - rechthoek.Left;
                    var hoogte = rechthoek.Bottom - rechthoek.Top;

                    if (breedte < MinimaleZijde || hoogte < MinimaleZijde || !HeeftTitel(venster))
                    {
                        continue;
                    }

                    var oppervlak = (long)breedte * hoogte;
                    if (oppervlak > besteOppervlak)
                    {
                        besteOppervlak = oppervlak;
                        beste = venster;
                    }
                }
            }
            finally
            {
                proces.Dispose();
            }
        }

        return beste;
    }

    private static bool HeeftTitel(IntPtr venster)
    {
        var lengte = GetWindowTextLength(venster);
        if (lengte <= 0)
        {
            return false;
        }

        var titel = new StringBuilder(lengte + 1);
        GetWindowText(venster, titel, titel.Capacity);

        // WPF hangt er zelf een paar vensters met een vaste naam aan; die
        // zijn klein, maar we sluiten ze ook op naam uit voor de zekerheid.
        var tekst = titel.ToString();
        return tekst.Length > 0
            && !tekst.Equals("SystemResourceNotifyWindow", StringComparison.Ordinal)
            && !tekst.Equals("MediaContextNotificationWindow", StringComparison.Ordinal);
    }

    /// <summary>Alle vensters op het hoogste niveau van dit proces, zichtbaar of niet.</summary>
    private static List<IntPtr> VenstersVan(int processId)
    {
        var gevonden = new List<IntPtr>();

        EnumWindows((venster, _) =>
        {
            GetWindowThreadProcessId(venster, out var pid);
            if (pid == processId && GetWindow(venster, GetWindowOwner) == IntPtr.Zero)
            {
                gevonden.Add(venster);
            }

            return true;
        }, IntPtr.Zero);

        return gevonden;
    }

    private const int ShowHide = 0;
    private const int ShowNormal = 1;
    private const int ShowMinimized = 2;
    private const int ShowNoActivate = 4;
    private const uint GetWindowOwner = 4;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowPlacement
    {
        public int length;
        public int flags;
        public int showCmd;
        public Point ptMinPosition;
        public Point ptMaxPosition;
        public Rect rcNormalPosition;
    }
}

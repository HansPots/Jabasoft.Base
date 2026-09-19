namespace Jabasoft.Base;

/// <summary>
/// De procesnamen van de applicaties in de familie.
///
/// Stond eerder als voorvoegsel "Jabasoft." in AiBrokerProcessLauncher, en
/// dat ging mis zodra er een tweede applicatie kwam: LocalAiStudio.App en
/// Stylebook.Playground beginnen daar niet mee. Gevolg was dat Jabasoft bij
/// het afsluiten dacht dat hij de laatste was en de broker meenam, terwijl
/// LocalAiStudio er nog op leunde.
///
/// Een uitgeschreven lijst dus, in plaats van een regel die toevallig
/// klopte. Komt er een applicatie bij, dan hoort hij hier - dat is een
/// bewuste handeling en geen naamkundig toeval.
/// </summary>
public static class JabasoftApps
{
    /// <summary>Zonder .exe: zoals Process.ProcessName ze teruggeeft.</summary>
    public static readonly IReadOnlyList<string> ProcessNames =
    [
        "Jabasoft.App",
        "LocalAiStudio.App",
        "TabStudio.App",
        "Stylebook.Playground",
    ];

    /// <summary>Hoort dit proces bij de familie?</summary>
    public static bool IsFamily(string processName) =>
        ProcessNames.Contains(processName, StringComparer.OrdinalIgnoreCase);
}

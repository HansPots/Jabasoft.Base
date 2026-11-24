using System.Diagnostics;
using System.Reflection;

namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Starts Jabasoft.Broker if it isn't already running. Every JabaSoft app
/// calls <see cref="EnsureRunningAsync"/> once at its own startup -
/// whichever app happens to run first is the one that actually starts it,
/// the others just find it already reachable.
///
/// Een app stopt de broker NOOIT (besloten 24-11-2026). De broker is een
/// losse dienst die ook Goose, Hermes, Claude en Jabasoft.Brug gebruiken;
/// een app die afsluit weet niet wie hem verder nog nodig heeft. Eerder
/// stopte <see cref="StopIfUnused"/> hem als de app dacht de laatste te
/// zijn, maar die telling klopte niet (de Brug stond niet in de lijst) en
/// trok de broker onder de Brug vandaan. Stoppen gaat alleen nog via het
/// traypictogram of de beheerscripts in Jabasoft.Broker/Beheer.
///
/// Known limitation: a plain <see cref="Process.Start(ProcessStartInfo)"/>
/// child can still be killed if the *launching* process itself runs inside
/// a Windows Job Object with JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE (some IDE
/// debuggers set this up) - not handled here since the normal usage
/// pattern (double-clicking the built app, or `dotnet run` from a plain
/// terminal) doesn't hit this.
/// </summary>
public static class AiBrokerProcessLauncher
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(45);

    /// <summary>
    /// De map van het Jabasoft.Broker-project: naast deze repo, dus
    /// &lt;map van Jabasoft.Base&gt;\..\Jabasoft.Broker. De map van
    /// Jabasoft.Base wordt bij het bouwen vastgelegd (zie Jabasoft.Base.csproj)
    /// in plaats van hier uitgeschreven: een vast pad als D:\GitHub\... ging
    /// mis zodra de repo's ergens anders kwamen te staan, en dan startte de
    /// broker stilletjes niet - met "database niet bereikbaar" als gevolg.
    /// </summary>
    public static readonly string DefaultProjectPath = Path.GetFullPath(
        Path.Combine(ProjectMap(), "..", "Jabasoft.Broker"));

    private static string ProjectMap() =>
        typeof(AiBrokerProcessLauncher).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "ProjectMap")?.Value
        ?? AppContext.BaseDirectory;

    public static async Task EnsureRunningAsync(
        string baseUrl = AiBrokerClient.DefaultBaseUrl,
        string? projectPath = null,
        CancellationToken cancellationToken = default)
    {
        projectPath ??= DefaultProjectPath;

        using var probeClient = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
        if (await IsHealthyAsync(probeClient, baseUrl).ConfigureAwait(false))
        {
            return;
        }

        if (!Directory.Exists(projectPath))
        {
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = projectPath,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--no-launch-profile");
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(baseUrl);
        startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";

        try
        {
            // Not stored anywhere, not awaited beyond Start() itself - this
            // is the one and only place that's allowed to matter, since
            // nothing else in any app should ever reference this Process
            // object (that's what would let something kill it later).
            Process.Start(startInfo);
        }
        catch
        {
            return;
        }

        using var pollClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow.Add(StartupTimeout);
        while (DateTime.UtcNow < deadline)
        {
            if (await IsHealthyAsync(pollClient, baseUrl).ConfigureAwait(false))
            {
                return;
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Deed vroeger: de broker stoppen als deze app de laatste was. Doet nu
    /// bewust NIETS - een app stopt de broker nooit meer (zie de toelichting
    /// bovenaan deze klasse). Blijft bestaan zodat de apps die dit bij het
    /// afsluiten nog aanroepen, gewoon blijven bouwen.
    /// </summary>
    public static void StopIfUnused()
    {
    }

    private static async Task<bool> IsHealthyAsync(HttpClient client, string baseUrl)
    {
        try
        {
            using var response = await client.GetAsync($"{baseUrl.TrimEnd('/')}/health").ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

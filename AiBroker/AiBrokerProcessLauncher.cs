using System.Diagnostics;

namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Starts Jabasoft.Broker if it isn't already running. Every JabaSoft app
/// calls <see cref="EnsureRunningAsync"/> once at its own startup -
/// whichever app happens to run first is the one that actually starts it,
/// the others just find it already reachable.
///
/// Bij het afsluiten roept een app <see cref="StopIfUnused"/> aan: is
/// hij de laatste JabaSoft-applicatie die draait, dan gaat de broker mee
/// uit. Draait er nog een andere, dan blijft hij staan - die heeft hem
/// immers nodig. Zo blijft er na een dag werken geen brokerproces hangen,
/// zonder dat een app de broker onder een ander zijn voeten wegtrekt.
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

    public static async Task EnsureRunningAsync(
        string baseUrl = AiBrokerClient.DefaultBaseUrl,
        string projectPath = @"C:\Repos\Jabasoft.Broker",
        CancellationToken cancellationToken = default)
    {
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

    /// <summary>De procesnaam van de broker - waarop we hem terugvinden om hem te stoppen.</summary>
    private const string BrokerProcessName = "Jabasoft.Broker";

    /// <summary>
    /// Stopt de broker, maar alleen als deze applicatie de laatste is die
    /// hem nog nodig had. Roep dit aan bij het afsluiten.
    ///
    /// "Nog een andere applicatie" = een draaiend proces waarvan de naam met
    /// <paramref name="familyPrefix"/> begint, de broker zelf en dit proces
    /// niet meegerekend. Dat is een simpele regel die op deze machine klopt
    /// omdat de hele familie Jabasoft.* heet; hernoem je een app, denk er
    /// dan aan dat hij hier onder valt.
    /// </summary>
    public static void StopIfUnused(string familyPrefix = "Jabasoft.")
    {
        var self = Environment.ProcessId;

        var anderen = Process.GetProcesses()
            .Where(p => p.Id != self
                && !string.Equals(p.ProcessName, BrokerProcessName, StringComparison.OrdinalIgnoreCase)
                && p.ProcessName.StartsWith(familyPrefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var proces in anderen)
        {
            proces.Dispose();
        }

        if (anderen.Count > 0)
        {
            return;
        }

        foreach (var broker in Process.GetProcessesByName(BrokerProcessName))
        {
            try
            {
                broker.Kill();
            }
            catch (Exception)
            {
                // Al gestopt, of we mogen er niet bij. Niets aan te doen bij
                // het afsluiten, en zeker niets om de app voor op te houden.
            }
            finally
            {
                broker.Dispose();
            }
        }
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

using System.Diagnostics;

namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Starts Jabasoft.Broker if it isn't already running. Every JabaSoft app
/// calls <see cref="EnsureRunningAsync"/> once at its own startup -
/// whichever app happens to run first is the one that actually starts it,
/// the others just find it already reachable.
///
/// The broker process started here is NEVER tracked or killed by anything
/// in this codebase, on purpose, so it keeps running after every app
/// closes - the user stops it manually (Task Manager, or a future "stop
/// broker" affordance) when they actually want it gone.
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
        if (await IsHealthyAsync(probeClient, baseUrl))
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
            if (await IsHealthyAsync(pollClient, baseUrl))
            {
                return;
            }

            await Task.Delay(500, cancellationToken);
        }
    }

    private static async Task<bool> IsHealthyAsync(HttpClient client, string baseUrl)
    {
        try
        {
            using var response = await client.GetAsync($"{baseUrl.TrimEnd('/')}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

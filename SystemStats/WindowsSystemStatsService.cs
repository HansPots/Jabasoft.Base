using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Jabasoft.Base.SystemStats;

/// <summary>
/// Reads live CPU/RAM via Windows APIs (kernel32 P/Invoke - no extra NuGet
/// package needed, every JabaSoft app only ever runs on Windows) and GPU
/// VRAM via the "nvidia-smi" command-line tool that ships with NVIDIA
/// drivers. Register as a singleton: CPU usage is delta-based, it needs to
/// remember the previous GetSystemTimes() sample between calls.
///
/// No NVIDIA GPU/driver, or "nvidia-smi" not on PATH? VramUsedBytes/
/// VramTotalBytes just come back null - callers should show "n.b." for that.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsSystemStatsService : ISystemStatsService
{
    private readonly object _cpuLock = new();
    private (ulong Idle, ulong Kernel, ulong User)? _previousCpuTimes;

    /// <inheritdoc />
    public async Task<SystemStatsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var cpuPercent = GetCpuPercent();
        var (ramUsed, ramTotal) = GetRamBytes();
        var (vramUsed, vramTotal) = await GetVramBytesAsync(cancellationToken);

        return new SystemStatsSnapshot(cpuPercent, ramUsed, ramTotal, vramUsed, vramTotal);
    }

    private double GetCpuPercent()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
        {
            return 0;
        }

        var current = (ToUInt64(idle), ToUInt64(kernel), ToUInt64(user));

        lock (_cpuLock)
        {
            var previous = _previousCpuTimes;
            _previousCpuTimes = current;

            if (previous is null)
            {
                // First call ever: no delta yet to compute a percentage from.
                return 0;
            }

            var idleDelta = current.Item1 - previous.Value.Idle;
            // GetSystemTimes' "kernel" time already includes idle time.
            var totalDelta = (current.Item2 - previous.Value.Kernel) + (current.Item3 - previous.Value.User);

            if (totalDelta == 0)
            {
                return 0;
            }

            var busy = totalDelta - idleDelta;
            return Math.Clamp((double)busy / totalDelta * 100, 0, 100);
        }
    }

    private static (long UsedBytes, long TotalBytes) GetRamBytes()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref status))
        {
            return (0, 0);
        }

        var total = (long)status.ullTotalPhys;
        var available = (long)status.ullAvailPhys;
        return (total - available, total);
    }

    private static async Task<(long? UsedBytes, long? TotalBytes)> GetVramBytesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo("nvidia-smi", "--query-gpu=memory.used,memory.total --format=csv,noheader,nounits")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return (null, null);
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

            var output = await process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            await process.WaitForExitAsync(timeoutCts.Token);

            if (process.ExitCode != 0)
            {
                return (null, null);
            }

            // First GPU only ("memory.used, memory.total" in MiB, one line per GPU).
            var firstLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (firstLine is null)
            {
                return (null, null);
            }

            var parts = firstLine.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 ||
                !long.TryParse(parts[0], out var usedMiB) ||
                !long.TryParse(parts[1], out var totalMiB))
            {
                return (null, null);
            }

            return (usedMiB * 1024 * 1024, totalMiB * 1024 * 1024);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or OperationCanceledException or IOException)
        {
            // nvidia-smi not installed/not on PATH, or it hung/timed out - no NVIDIA GPU here.
            return (null, null);
        }
    }

    private static ulong ToUInt64(System.Runtime.InteropServices.ComTypes.FILETIME time) => ((ulong)(uint)time.dwHighDateTime << 32) | (uint)time.dwLowDateTime;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out System.Runtime.InteropServices.ComTypes.FILETIME lpIdleTime, out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime, out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
}

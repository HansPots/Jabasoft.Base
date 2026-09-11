namespace Jabasoft.Base.SystemStats;

/// <summary>
/// A snapshot of live system resource usage - CPU load, RAM, and GPU
/// ("Model") VRAM - for a shell footer. VRAM is null when it couldn't be
/// determined (e.g. no NVIDIA GPU/driver present).
/// </summary>
public sealed record SystemStatsSnapshot(
    double CpuPercent,
    long RamUsedBytes,
    long RamTotalBytes,
    long? VramUsedBytes,
    long? VramTotalBytes);

/// <summary>Reads live system resource usage for a shell footer's meters.</summary>
public interface ISystemStatsService
{
    /// <summary>Takes a fresh reading of CPU/RAM/VRAM usage.</summary>
    Task<SystemStatsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}

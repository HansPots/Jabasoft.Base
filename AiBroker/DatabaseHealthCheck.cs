using Jabasoft.Base.Health;
using Jabasoft.Base.Logging;

namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Controleert bij het opstarten of de gedeelde database er is: de broker
/// probeert JabasoftBase te openen en kijkt of de tokentabel erin staat.
///
/// De applicatie vraagt dat aan de broker en praat dus zelf niet met de
/// database - net als bij de AI-instelling ligt die verbinding op één
/// plek. Gevolg: deze controle hoort ACHTER <see cref="AiBrokerHealthCheck"/>
/// in de rij, want zonder draaiende broker valt er niets te vragen.
///
/// Wat er precies mis is (verkeerde server, database weg, tabel weg) gaat
/// naar het activiteitenlog; de pil zelf zegt alleen "Error".
/// </summary>
public sealed class DatabaseHealthCheck(IAiBrokerClient client, ActivityLog? log = null) : IHealthCheck
{
    private readonly ActivityLog _log = log ?? ActivityLog.Shared;

    /// <inheritdoc />
    public string Name => "database";

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);

        var status = await client.GetUsageStatusAsync(cancellationToken).ConfigureAwait(false);
        if (status.Available)
        {
            return true;
        }

        _log.Add("app", $"Database niet bereikbaar: {status.Message}");
        return false;
    }
}

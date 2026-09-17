using Jabasoft.Base.Health;

namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Controleert of de AI-SERVER zelf bereikbaar is: LM Studio of Ollama, op
/// het adres dat in de instelling staat.
///
/// Apart van <see cref="AiModelsHealthCheck"/> met opzet. Dat zijn twee
/// verschillende storingen die om verschillende dingen vragen: staat de
/// server uit, dan moet je hem aanzetten; draait hij wel maar mist het
/// model, dan moet je een ander model kiezen. In het gezondheidsoverzicht
/// wil je dat verschil zien.
///
/// De broker doet het opvragen; deze controle kijkt alleen of daar een
/// lijst uitkwam. Zo praat ook dit niet rechtstreeks met LM Studio.
/// </summary>
public sealed class AiServerHealthCheck(IAiBrokerClient client) : IHealthCheck
{
    /// <inheritdoc />
    public string Name => "AI server";

    /// <inheritdoc />
    public async Task<HealthCheckOutcome> CheckAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);

        var settings = await client.GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (!settings.Success)
        {
            return HealthCheckOutcome.Fout($"Instelling niet op te halen: {settings.ErrorMessage}");
        }

        progress.Report($"Checking {settings.Settings.Provider}");

        var lijst = await client.ListConfiguredModelsAsync(cancellationToken).ConfigureAwait(false);

        return lijst.Success
            ? HealthCheckOutcome.Ok($"{settings.Settings.Provider} op {settings.Settings.ActiveServerUrl} - {lijst.Models.Count} modellen")
            : HealthCheckOutcome.Fout($"{settings.Settings.Provider} op {settings.Settings.ActiveServerUrl}: {lijst.ErrorMessage}");
    }
}

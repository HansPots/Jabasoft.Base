# Jabasoft.Base

Kale, UI-loze class library met code die meerdere JabaSoft-apps (en de
headless `Jabasoft.Broker`) delen:

- **`AiBroker/`** - de contracts (`IAiBrokerClient`, `AiBrokerClient`,
  `AiProvider`, request/result-records) waarmee elke app met
  `Jabasoft.Broker` praat in plaats van rechtstreeks met Ollama/LM Studio,
  plus `AiBrokerProcessLauncher` (start de broker automatisch als die nog
  niet draait).
- **`SystemStats/`** - `ISystemStatsService`/`WindowsSystemStatsService`
  voor CPU/RAM/VRAM-metingen, herbruikbaar voor een eventuele shell-footer.

Dit project bevat zelf geen UI (geen WPF, geen Blazor/Razor) - elke app die
er iets mee wil tonen (bijv. een token-verbruikscherm) bouwt dat zelf, als
eigen WPF-control met `Stylebook.Components`-styling.

## Hergebruik door een app

```xml
<ProjectReference Include="..\Jabasoft.Base\Jabasoft.Base.csproj" />
```

`Shared.Telemetry` (waar tokenverbruik daadwerkelijk wordt weggeschreven)
leeft in `Jabasoft.Stylebook` en wordt alleen door `Jabasoft.Broker`
zelf gerefereerd - apps praten met de broker via `IAiBrokerClient`, niet
rechtstreeks met de telemetrie-database.

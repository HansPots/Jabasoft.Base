# Jabasoft.Base

De ene canonieke plek voor Razor-componenten die door meerdere JabaSoft-apps
gedeeld worden (Blazor Server én Blazor Hybrid). Startpunt: `TokenUsageOverview`,
het token-verbruikscherm (totalen + per week inklapbare details) dat Jabasoft,
JabaSoft.TabStudio en JabaSoftLocalAiStudio alle drie embedden.

Oorspronkelijk een project binnen `Jabasoft.Shared`; als los repo hier
neergezet zodat het als eigenstandig, zelfstandig te bouwen project
te vinden is naast de andere JabaSoft-repo's.

## Hergebruik door een app

```xml
<ProjectReference Include="..\..\Jabasoft.Base\Jabasoft.Base.csproj" />
```

En in de host-pagina (naast de eigen `*.styles.css`):

```html
<link rel="stylesheet" href="_content/Jabasoft.Base/Jabasoft.Base.bundle.scp.css" />
```

Componenten in dit project gebruiken `Shared.Telemetry` (uit het
`Jabasoft.Shared`-repo, als sibling-map onder `C:\Repos`) voor
databasetoegang - beide repo's moeten dus naast elkaar staan.

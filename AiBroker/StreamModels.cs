namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Eén stukje van een antwoord dat nog binnenkomt.
///
/// Een streamend antwoord komt in tientallen tot honderden van deze
/// stukjes binnen: eerst een reeks met alleen <see cref="Text"/> erin, en
/// als laatste één met <see cref="Done"/> op true. In die laatste staan de
/// tokens die het gekost heeft - eerder weet de server dat zelf ook niet.
///
/// Gaat er onderweg iets mis, dan komt er een stukje met
/// <see cref="ErrorMessage"/> en <see cref="Done"/> op true; er komt er
/// daarna geen meer.
///
/// <see cref="Thinking"/> markeert een stukje waarin een model hardop
/// nadenkt en dat dus NIET op het scherm hoort. Het telt wel mee: een
/// redenerend model kan tientallen seconden bezig zijn voordat het eerste
/// echte woord komt, en zonder deze stukjes zou een teller al die tijd
/// stilstaan alsof er niets gebeurt.
/// </summary>
public sealed record ChatStreamChunk(
    string? Text = null,
    bool Done = false,
    long PromptTokens = 0,
    long CompletionTokens = 0,
    string? ErrorMessage = null,
    bool Thinking = false);

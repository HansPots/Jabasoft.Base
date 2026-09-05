namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Which local LLM server a request targets. The one canonical copy - both
/// JabaSoft.TabStudio and JabaSoftLocalAiStudio used to each define their
/// own copy of this enum next to their own provider-specific HTTP calls;
/// now every app references this one instead, and the actual provider
/// dispatch (Ollama vs LM Studio's different REST shapes) lives once in
/// Jabasoft.Broker.
/// </summary>
public enum AiProvider
{
    Ollama,
    LmStudio,
}

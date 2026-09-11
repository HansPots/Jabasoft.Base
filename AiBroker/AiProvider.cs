namespace Jabasoft.Base.AiBroker;

/// <summary>
/// Which local LLM server a request targets. The one canonical copy -
/// every app references this instead of defining its own; the actual
/// provider dispatch (Ollama vs LM Studio's different REST shapes) lives
/// once, in Jabasoft.Broker.
/// </summary>
public enum AiProvider
{
    Ollama,
    LmStudio,
}

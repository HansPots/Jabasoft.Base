namespace Jabasoft.Base.AiBroker;

/// <summary>One turn in a chat conversation. Role is "system", "user", or "assistant".</summary>
public sealed record ChatMessage(string Role, string Content);

/// <summary>
/// A chat request sent to Jabasoft.Broker. <see cref="Application"/> is the
/// calling app's own name (e.g. "TabStudio", "LocalAiStudio") - the broker
/// uses it to attribute token usage when it records the call to the shared
/// JabasoftBase database, so apps no longer record their own usage.
/// </summary>
public sealed record ChatRequest(
    AiProvider Provider,
    string ServerUrl,
    string Model,
    IReadOnlyList<ChatMessage> Messages,
    string Application,
    double? Temperature = null);

/// <summary>
/// The outcome of a chat request. Ollama reports prompt/completion tokens
/// separately; LM Studio only reports a total - callers that only have a
/// total put it all in <see cref="PromptTokens"/> with
/// <see cref="CompletionTokens"/> left at 0 (matches how apps already
/// recorded LM Studio usage before this moved into the broker).
/// </summary>
public sealed record ChatResult(bool Success, string Reply, string? ErrorMessage, long PromptTokens = 0, long CompletionTokens = 0);

/// <summary>An embedding request sent to Jabasoft.Broker.</summary>
public sealed record EmbedRequest(AiProvider Provider, string ServerUrl, string Model, string Text, string Application);

/// <summary>The outcome of an embedding request.</summary>
public sealed record EmbedResult(bool Success, float[] Vector, string? ErrorMessage, long TokensUsed = 0);

/// <summary>A model-listing or reachability request - no request body needed, provider/serverUrl are query parameters.</summary>
public sealed record ModelListResult(bool Success, IReadOnlyList<string> Models, string? ErrorMessage);

/// <summary>A connection-test request sent to Jabasoft.Broker.</summary>
public sealed record TestConnectionRequest(AiProvider Provider, string ServerUrl, string Model);

/// <summary>The outcome of a connection test.</summary>
public sealed record ConnectionTestResult(bool Success, string Message);

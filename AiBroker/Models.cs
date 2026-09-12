namespace Jabasoft.Base.AiBroker;

/// <summary>One turn in a chat conversation. Role is "system", "user", or "assistant".</summary>
public sealed record ChatMessage(string Role, string Content);

/// <summary>
/// A chat request sent to Jabasoft.Broker. <see cref="Application"/> is the
/// calling app's own name (e.g. "Stylebook", "TabStudio") - identifies which
/// app made the call, for whenever the broker attributes usage again.
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
/// <see cref="CompletionTokens"/> left at 0.
/// </summary>
public sealed record ChatResult(bool Success, string Reply, string? ErrorMessage, long PromptTokens = 0, long CompletionTokens = 0);

/// <summary>An embedding request sent to Jabasoft.Broker.</summary>
public sealed record EmbedRequest(AiProvider Provider, string ServerUrl, string Model, string Text, string Application);

/// <summary>The outcome of an embedding request.</summary>
public sealed record EmbedResult(bool Success, float[] Vector, string? ErrorMessage, long TokensUsed = 0);

/// <summary>A model-listing/reachability result - the broker caches this per (provider, serverUrl) for a short TTL.</summary>
public sealed record ModelListResult(bool Success, IReadOnlyList<string> Models, string? ErrorMessage);

/// <summary>A connection-test request sent to Jabasoft.Broker.</summary>
public sealed record TestConnectionRequest(AiProvider Provider, string ServerUrl, string Model);

/// <summary>The outcome of a connection test.</summary>
public sealed record ConnectionTestResult(bool Success, string Message);

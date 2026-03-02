using Microsoft.Extensions.AI;

namespace Tests.Utilities;

/// <summary>
/// Assertion helpers that inspect a <see cref="ChatResponse"/> for evidence
/// of tool use. When <see cref="FunctionInvokingChatClient"/> middleware is
/// active, it appends <see cref="FunctionCallContent"/> and
/// <see cref="FunctionResultContent"/> entries to the response messages.
/// These helpers examine those entries directly — no interception required.
/// </summary>
public static class ResponseAssertions
{
    /// <summary>All <see cref="FunctionCallContent"/> entries in the response.</summary>
    public static IReadOnlyList<FunctionCallContent> GetToolCalls(ChatResponse response) =>
        response.Messages
            .SelectMany(m => m.Contents)
            .OfType<FunctionCallContent>()
            .ToList();

    /// <summary>All <see cref="FunctionResultContent"/> entries in the response.</summary>
    public static IReadOnlyList<FunctionResultContent> GetToolResults(ChatResponse response) =>
        response.Messages
            .SelectMany(m => m.Contents)
            .OfType<FunctionResultContent>()
            .ToList();

    /// <summary>Distinct tool names that were called.</summary>
    public static IReadOnlyList<string> GetCalledToolNames(ChatResponse response) =>
        GetToolCalls(response)
            .Select(c => c.Name)
            .Distinct()
            .ToList();

    /// <summary>
    /// Asserts that a specific tool was called at least once.
    /// Throws <see cref="Xunit.Sdk.ContainsException"/> on failure.
    /// </summary>
    public static void AssertToolWasCalled(ChatResponse response, string toolName)
    {
        var calls = GetToolCalls(response);
        Assert.Contains(calls, c => c.Name == toolName);
    }

    /// <summary>
    /// Asserts that a specific tool produced at least one result.
    /// </summary>
    public static void AssertToolHasResult(ChatResponse response, string toolName)
    {
        var calls = GetToolCalls(response);
        var callIds = calls.Where(c => c.Name == toolName).Select(c => c.CallId).ToHashSet();
        var results = GetToolResults(response);
        Assert.Contains(results, r => callIds.Contains(r.CallId));
    }
}

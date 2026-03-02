using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.AI;

namespace Library.Domain.Models.Evaluation;

public sealed record BasicScore(
    double TaskSuccess,
    double Quality,
    double Hallucination,
    double Overall,
    string[] Notes
);

public sealed record EvaluationResult(BasicScore Score);

public sealed class AgentRunResponse<TResponse>
{
    public TResponse? Value { get; init; }
    public string? Text { get; init; }
    public IReadOnlyList<ChatMessage>? Messages { get; init; }

    public static AgentRunResponse<TResponse> FromValue(TResponse value, IEnumerable<ChatMessage> messages) =>
        new() { Value = value, Messages = messages.ToList() };

    public static AgentRunResponse<string> FromText(string text, IEnumerable<ChatMessage> messages) =>
        new() { Text = text, Messages = messages.ToList(), Value = text };
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Library.Domain.Models.Evaluation;

namespace Library.Application.Services;

public sealed class AgentEvaluator
{
    private readonly IChatClient _judgeClient;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AgentEvaluator(IChatClient judgeClient)
    {
        _judgeClient = judgeClient ?? throw new ArgumentNullException(nameof(judgeClient));
    }

    public Task<EvaluationResult> EvaluateAsync<TResponse>(
        string originalSystemPrompt,
        string originalUserInput,
        AgentRunResponse<TResponse> agentResponse,
        string evaluationRubric,
        CancellationToken cancellationToken = default)
    {
        if (agentResponse == null) throw new ArgumentNullException(nameof(agentResponse));

        string? structuredJson = null;
        string? textOutput = null;

        if (typeof(TResponse) == typeof(string))
        {
            textOutput = agentResponse.Value as string ?? agentResponse.Text;
        }
        else
        {
            if (agentResponse.Value != null)
            {
                structuredJson = JsonSerializer.Serialize(agentResponse.Value, _jsonOptions);
            }
            textOutput = agentResponse.Text;
        }

        return EvaluateInternalAsync(
            originalSystemPrompt,
            originalUserInput,
            agentResponse.Messages ?? Array.Empty<ChatMessage>(),
            structuredJson,
            textOutput,
            evaluationRubric,
            cancellationToken);
    }

    private async Task<EvaluationResult> EvaluateInternalAsync(
        string originalSystemPrompt,
        string originalUserInput,
        IReadOnlyList<ChatMessage> messages,
        string? structuredValueJson,
        string? textOutput,
        string rubric,
        CancellationToken cancellationToken)
    {
        var evaluationPrompt = BuildEvaluationPrompt(
            originalSystemPrompt,
            originalUserInput,
            messages,
            structuredValueJson,
            textOutput,
            rubric);

        var systemMessage = new ChatMessage(ChatRole.System, "You are a strict evaluation system. Return ONLY valid JSON.");
        var userMessage = new ChatMessage(ChatRole.User, evaluationPrompt);

        var response = await _judgeClient.GetResponseAsync(new[] { systemMessage, userMessage }, 
            new ChatOptions { Temperature = 0 }, cancellationToken);
        
        var rawEvaluation = response.Text;

        if (string.IsNullOrWhiteSpace(rawEvaluation))
            throw new InvalidOperationException("Judge returned an empty response.");

        // Minimal cleaning if the LLM wraps in markdown code blocks
        var cleanedJson = rawEvaluation.Trim();
        if (cleanedJson.StartsWith("```json")) cleanedJson = cleanedJson[7..].Trim();
        if (cleanedJson.StartsWith("```")) cleanedJson = cleanedJson[3..].Trim();
        if (cleanedJson.EndsWith("```")) cleanedJson = cleanedJson[..^3].Trim();

        try
        {
            var score = JsonSerializer.Deserialize<BasicScore>(cleanedJson, _jsonOptions);
            if (score == null) throw new InvalidOperationException("Judge returned null after deserialization.");
            return new EvaluationResult(score);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Judge output was not valid JSON: {rawEvaluation}", ex);
        }
    }

    private static string BuildEvaluationPrompt(
        string originalSystemPrompt,
        string originalUserInput,
        IReadOnlyList<ChatMessage> messages,
        string? structuredValueJson,
        string? textOutput,
        string rubric)
    {
        var sb = new StringBuilder();

        var messagesBlock = string.Join("\n", messages.Select(m =>
            $"<message role=\"{m.Role}\">{m.Text}</message>"));

        sb.AppendLine("<evaluation>");
        sb.AppendLine("  <original_system_prompt>");
        sb.AppendLine(originalSystemPrompt);
        sb.AppendLine("  </original_system_prompt>");

        sb.AppendLine("  <original_user_input>");
        sb.AppendLine(originalUserInput);
        sb.AppendLine("  </original_user_input>");

        sb.AppendLine("  <agent_messages>");
        sb.AppendLine(messagesBlock);
        sb.AppendLine("  </agent_messages>");

        if (!string.IsNullOrWhiteSpace(structuredValueJson))
        {
            sb.AppendLine("  <structured_output>");
            sb.AppendLine(structuredValueJson);
            sb.AppendLine("  </structured_output>");
        }

        sb.AppendLine("  <text_output>");
        sb.AppendLine(textOutput ?? string.Empty);
        sb.AppendLine("  </text_output>");

        sb.AppendLine("  <rubric>");
        sb.AppendLine(rubric);
        sb.AppendLine("  </rubric>");

        sb.AppendLine(@"  <instructions>
    Return STRICT JSON matching this schema:
    {
      ""taskSuccess"": 0.0,
      ""quality"": 0.0,
      ""hallucination"": 0.0,
      ""overall"": 0.0,
      ""notes"": [""...""]
    }

    Rules:
    - Scores are between 0.0 and 1.0
    - Be strict
    - Do not add extra fields
    - Do not include XML or markdown formatting in your response
  </instructions>");

        sb.AppendLine("</evaluation>");

        return sb.ToString();
    }
}

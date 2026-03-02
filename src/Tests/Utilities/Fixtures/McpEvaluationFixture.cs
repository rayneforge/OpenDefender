using System;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Library.Domain.Models.Evaluation;
using Library.Application.Evaluators;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Utilities.Fixtures;

public sealed class McpEvaluationFixture
{
    private readonly AgentEvaluator _evaluator;
    private readonly ITestOutputHelper _output;

    public McpEvaluationFixture(IChatClient judgeClient, ITestOutputHelper output)
    {
        _evaluator = new AgentEvaluator(judgeClient);
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public async Task<EvaluationResult> EvaluateAsync<TResponse>(
        string systemPrompt,
        string userInput,
        AgentRunResponse<TResponse> response,
        string rubric)
    {
        var result = await _evaluator.EvaluateAsync(systemPrompt, userInput, response, rubric);
        LogEvaluationResult(result);
        return result;
    }

    public McpEvaluationFixture Expect(EvaluationResult result, Func<EvaluationResult, bool> predicate, string failureMessage)
    {
        if (!predicate(result))
        {
            var notes = string.Join(" | ", result.Score.Notes);
            var errorMessage = $"{failureMessage}\n" +
                               $"Scores - Success: {result.Score.TaskSuccess:P}, Quality: {result.Score.Quality:P}, Hallucination: {result.Score.Hallucination:P}\n" +
                               $"Notes: {notes}";
            
            _output.WriteLine($"[FAILURE] {errorMessage}");
            Assert.Fail(errorMessage);
        }
        return this;
    }

    private void LogEvaluationResult(EvaluationResult result)
    {
        _output.WriteLine("");
        _output.WriteLine("=== Evaluation Result ===");
        _output.WriteLine($"Success:       {result.Score.TaskSuccess:P}");
        _output.WriteLine($"Quality:       {result.Score.Quality:P}");
        _output.WriteLine($"Hallucination: {result.Score.Hallucination:P}");
        _output.WriteLine($"Overall:       {result.Score.Overall:P}");
        _output.WriteLine($"Notes:         {string.Join(" | ", result.Score.Notes)}");
        _output.WriteLine("=========================");
    }
}

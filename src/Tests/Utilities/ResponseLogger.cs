using System.Text.Json;
using Microsoft.Extensions.AI;
using Xunit.Abstractions;

namespace Tests.Utilities;

/// <summary>
/// Writes a structured breakdown of a <see cref="ChatResponse"/> to xUnit's
/// <see cref="ITestOutputHelper"/> so every test run produces a readable log
/// of what the agent did — tools called, arguments sent, results returned,
/// and the final assistant text.
/// </summary>
public static class ResponseLogger
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>
    /// Logs the full response breakdown: tool calls, tool results, and final text.
    /// Call this immediately after <c>GetResponseAsync</c> in every test.
    /// </summary>
    public static void Log(ITestOutputHelper output, ChatResponse response)
    {
        output.WriteLine("═══════════════════════════════════════════════════");
        output.WriteLine("  AGENT RESPONSE BREAKDOWN");
        output.WriteLine("═══════════════════════════════════════════════════");

        // ── Tool Calls ──────────────────────────────────────────────────
        var calls = response.Messages
            .SelectMany(m => m.Contents)
            .OfType<FunctionCallContent>()
            .ToList();

        if (calls.Count > 0)
        {
            output.WriteLine($"\n🔧 Tool Calls ({calls.Count}):");
            foreach (var call in calls)
            {
                output.WriteLine($"  ┌─ {call.Name}  (CallId: {call.CallId})");
                if (call.Arguments is { Count: > 0 })
                {
                    foreach (var arg in call.Arguments)
                    {
                        var value = arg.Value is JsonElement je
                            ? JsonSerializer.Serialize(je, JsonOpts)
                            : arg.Value?.ToString() ?? "null";
                        output.WriteLine($"  │  {arg.Key}: {value}");
                    }
                }
                else
                {
                    output.WriteLine("  │  (no arguments)");
                }
                output.WriteLine("  └─");
            }
        }
        else
        {
            output.WriteLine("\n⚠️  No tool calls detected.");
        }

        // ── Tool Results ────────────────────────────────────────────────
        var results = response.Messages
            .SelectMany(m => m.Contents)
            .OfType<FunctionResultContent>()
            .ToList();

        if (results.Count > 0)
        {
            output.WriteLine($"\n📦 Tool Results ({results.Count}):");
            foreach (var result in results)
            {
                output.WriteLine($"  ┌─ CallId: {result.CallId}");
                var text = result.Result?.ToString() ?? "(null)";
                // Truncate long results for readability
                if (text.Length > 2000)
                    text = text[..2000] + $"\n  │  ... truncated ({text.Length} chars total)";
                output.WriteLine($"  │  {text}");
                output.WriteLine("  └─");
            }
        }

        // ── Final Assistant Text ────────────────────────────────────────
        var assistantText = response.Messages
            .Where(m => m.Role == ChatRole.Assistant)
            .SelectMany(m => m.Contents)
            .OfType<TextContent>()
            .Select(t => t.Text)
            .LastOrDefault();

        output.WriteLine("\n💬 Final Assistant Response:");
        output.WriteLine("───────────────────────────────────────────────────");
        output.WriteLine(assistantText ?? "(no text response)");
        output.WriteLine("───────────────────────────────────────────────────");

        // ── Summary ─────────────────────────────────────────────────────
        var calledNames = calls.Select(c => c.Name).Distinct().ToList();
        output.WriteLine($"\n📊 Summary: {calls.Count} call(s) → [{string.Join(", ", calledNames)}]");
        output.WriteLine("═══════════════════════════════════════════════════\n");
    }
}

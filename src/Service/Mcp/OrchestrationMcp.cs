using System.ComponentModel;
using ModelContextProtocol.Server;
using Library.Domain.Models.Queries;
using Library.Application.Tooling;

namespace Service.Mcp;

/// <summary>
/// Orchestration domain — diagnostic and analytics run history.
/// Raw: Orchestrations.
/// </summary>
[McpServerToolType]
[McpServerPromptType]
public static class OrchestrationMcp
{
    [McpServerTool(Name = "query_orchestrations", Title = "Query Orchestration History", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Query orchestration run history. Properties: Id (int), RunId (Guid), StartTime (DateTime). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryOrchestrations(QueryRequest request)
    {
        return await OrchestrationTools.QueryOrchestrations(request);
    }

    // ── Prompt ───────────────────────────────────────────────────────────

    [McpServerPrompt(Name = "orchestration-run-review", Title = "Orchestration Run Review")]
    [Description("Strategy for reviewing orchestration run history and data freshness.")]
    public static string OrchestrationRunReview() =>
        """
        You are reviewing observability pipeline health. Follow this strategy:
        1. Call query_orchestrations ordered by StartTime descending, top 5, to see the most recent diagnostic and analytics runs.
        2. Check the time gap between the latest StartTime and now. A gap greater than the expected cadence indicates a missed run.
        3. If runs are present, their RunId values serve as correlation keys - use them to scope queries in other domains to a specific collection window.
        4. If no runs exist or the latest is stale, FLAG as S2: the entire observability pipeline may be non-functional.
        """;
}

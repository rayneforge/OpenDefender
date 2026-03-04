using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Library.Domain.Models.Queries;
using Library.Infrastructure.Database;
using Library.Infrastructure.Query;

namespace Library.Application.Tooling;

/// <summary>
/// Orchestration tools — diagnostic and analytics run history.
/// </summary>
public static class OrchestrationTools
{
    public static IEnumerable<AITool> GetTools()
    {
        yield return AIFunctionFactory.Create(QueryOrchestrations, "query_orchestrations", "Query orchestration run history. Properties: Id (int), RunId (Guid), StartTime (DateTime). Supports structured filters, ordering, and paging.");
    }

    public static async Task<string> QueryOrchestrations(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.Orchestrations, request);
        return JsonSerializer.Serialize(results);
    }
}

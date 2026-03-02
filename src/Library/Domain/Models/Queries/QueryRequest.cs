namespace Library.Domain.Models.Queries;

/// <summary>
/// Strongly-typed query request for MCP tools.
/// Translates to expression-tree filters, ordering, and paging
/// without any OData parser or raw string dependency.
/// </summary>
public sealed class QueryRequest
{
    public FilterCondition[]? Filters { get; init; }
    public SortCondition[]? OrderBy { get; init; }
    public int? Top { get; init; }
    public int? Skip { get; init; }
}

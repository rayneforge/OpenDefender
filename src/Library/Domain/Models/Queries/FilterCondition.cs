namespace Library.Domain.Models.Queries;

/// <summary>
/// A single filter predicate. Field + Operator + Value.
/// Supported operators: eq, ne, gt, lt, ge, le, contains, startswith, endswith.
/// </summary>
public sealed class FilterCondition
{
    public string Field { get; init; } = default!;
    public string Operator { get; init; } = default!;
    public string Value { get; init; } = default!;
}

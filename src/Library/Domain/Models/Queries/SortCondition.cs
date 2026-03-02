namespace Library.Domain.Models.Queries;

/// <summary>
/// A single sort directive.
/// </summary>
public sealed class SortCondition
{
    public string Field { get; init; } = default!;
    public bool Descending { get; init; }
}

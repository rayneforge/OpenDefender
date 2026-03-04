using System;

namespace Library.Domain.Models.State;

/// <summary>
/// Tracks the last execution time for a <see cref="Library.Domain.Attributes.TaskTriggerAttribute"/>-decorated agent method.
/// The interval and prompt live on the method itself; this is just a schedule bookmark.
/// </summary>
public class AgentTask
{
    public int Id { get; set; }

    /// <summary>Matches <see cref="Library.Domain.Attributes.TaskTriggerAttribute.Name"/>.</summary>
    public required string Name { get; set; }

    /// <summary>Agent type name that owns this task (e.g. "ShieldAgent").</summary>
    public required string AgentName { get; set; }

    public DateTime? LastRun { get; set; }
}

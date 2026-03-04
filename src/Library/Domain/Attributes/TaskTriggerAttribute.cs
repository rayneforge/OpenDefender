using System;

namespace Library.Domain.Attributes;

/// <summary>
/// Marks an agent method as a scheduled task trigger.
/// The method must be parameterless and return <c>Task&lt;string&gt;</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class TaskTriggerAttribute : Attribute
{
    public string Name { get; }
    public string Description { get; }
    public TimeSpan Interval { get; }

    /// <param name="name">Unique task identifier, e.g. "daily_perimeter_audit".</param>
    /// <param name="description">Human-readable description.</param>
    /// <param name="interval">ISO 8601 duration or TimeSpan string, e.g. "1.00:00:00" for 1 day.</param>
    public TaskTriggerAttribute(string name, string description, string interval)
    {
        Name = name;
        Description = description;
        Interval = TimeSpan.Parse(interval);
    }
}

using System;

namespace Library.Domain.Attributes;

/// <summary>
/// Marks a method as a consumer of a named task trigger's output.
/// The method must accept exactly one parameter whose type matches
/// the return type of the corresponding <see cref="TaskTriggerAttribute"/> method.
/// Multiple consumers may subscribe to the same trigger name.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class TaskConsumerAttribute : Attribute
{
    /// <summary>
    /// The trigger name to subscribe to. Must match a <see cref="TaskTriggerAttribute.Name"/>.
    /// </summary>
    public string TriggerName { get; }

    /// <param name="triggerName">Name of the trigger to consume, e.g. "diagnostic_collection".</param>
    public TaskConsumerAttribute(string triggerName)
    {
        TriggerName = triggerName;
    }
}

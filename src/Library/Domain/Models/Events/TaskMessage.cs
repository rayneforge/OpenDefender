using System;

namespace Library.Domain.Models.Events;

/// <summary>
/// Envelope carried through the task channel.
/// The <see cref="Payload"/> type must match the consumer's parameter type.
/// </summary>
public sealed class TaskMessage
{
    /// <summary>
    /// Routing key — matches <see cref="Library.Domain.Attributes.TaskTriggerAttribute.Name"/>
    /// and <see cref="Library.Domain.Attributes.TaskConsumerAttribute.TriggerName"/>.
    /// </summary>
    public required string TriggerName { get; init; }

    /// <summary>
    /// The result produced by the trigger method (or published via <c>ITaskChannel</c>).
    /// May be null for void-like triggers.
    /// </summary>
    public object? Payload { get; init; }

    /// <summary>
    /// When the message was enqueued.
    /// </summary>
    public DateTime EnqueuedAtUtc { get; init; } = DateTime.UtcNow;
}

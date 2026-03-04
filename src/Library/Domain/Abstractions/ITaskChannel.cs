using System.Threading;
using System.Threading.Tasks;

namespace Library.Domain.Abstractions;

/// <summary>
/// Write-side API for the task dispatch pipeline.
/// Any component (hosted service, controller, orchestrator) can publish
/// a named message that will be routed to matching <c>[TaskConsumer]</c> methods.
/// </summary>
public interface ITaskChannel
{
    /// <summary>
    /// Publish a named payload into the dispatch pipeline.
    /// </summary>
    /// <param name="triggerName">Routing key matching a trigger/consumer name.</param>
    /// <param name="payload">The result object; type must match consumer parameter.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask PublishAsync(string triggerName, object? payload = null, CancellationToken ct = default);
}

using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Library.Domain.Abstractions;
using Library.Domain.Models.Events;

namespace Library.Infrastructure.MessageBus;

/// <summary>
/// In-process bounded channel that backs <see cref="ITaskChannel"/>.
/// Both the scheduler and external callers (controllers, orchestrators)
/// write here; the dispatcher reads.
/// </summary>
public sealed class TaskChannel : ITaskChannel
{
    private readonly Channel<TaskMessage> _channel;

    public ChannelReader<TaskMessage> Reader => _channel.Reader;

    public TaskChannel(int capacity = 64)
    {
        _channel = Channel.CreateBounded<TaskMessage>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    /// <inheritdoc />
    public ValueTask PublishAsync(string triggerName, object? payload = null, CancellationToken ct = default)
    {
        var message = new TaskMessage
        {
            TriggerName = triggerName,
            Payload = payload,
        };

        return _channel.Writer.WriteAsync(message, ct);
    }
}

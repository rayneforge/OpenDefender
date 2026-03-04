using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Library.Infrastructure.MessageBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Service.Services;

/// <summary>
/// Reads messages from the <see cref="TaskChannel"/> and invokes matching
/// <c>[TaskConsumer]</c> methods. Handles fan-out (multiple consumers per trigger)
/// and recursive chaining (consumer that is itself a trigger).
/// </summary>
public sealed class TaskDispatcherService : BackgroundService
{
    private readonly ILogger<TaskDispatcherService> _logger;
    private readonly TaskRegistry _registry;
    private readonly TaskChannel _channel;
    private readonly IServiceProvider _serviceProvider;

    public TaskDispatcherService(
        ILogger<TaskDispatcherService> logger,
        TaskRegistry registry,
        TaskChannel channel,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _registry = registry;
        _channel = channel;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TaskDispatcherService starting. Listening for messages...");

        await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            _logger.LogInformation("Dispatching '{TriggerName}' (payload: {Type})",
                message.TriggerName,
                message.Payload?.GetType().Name ?? "null");

            if (!_registry.Consumers.TryGetValue(message.TriggerName, out var consumers) || consumers.Count == 0)
            {
                _logger.LogDebug("No consumers for '{TriggerName}'.", message.TriggerName);
                continue;
            }

            foreach (var consumer in consumers)
            {
                try
                {
                    var result = await InvokeConsumerAsync(consumer, message.Payload, stoppingToken);

                    _logger.LogInformation("Consumer {Type}.{Method} handled '{TriggerName}'.",
                        consumer.DeclaringType.Name, consumer.Method.Name, message.TriggerName);

                    // If this consumer's method is also a [TaskTrigger], publish its output
                    // so downstream consumers can chain.
                    var triggerAttr = consumer.Method.GetCustomAttribute<Library.Domain.Attributes.TaskTriggerAttribute>();
                    if (triggerAttr is not null && result is not null)
                    {
                        await _channel.PublishAsync(triggerAttr.Name, result, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Consumer {Type}.{Method} failed for '{TriggerName}'.",
                        consumer.DeclaringType.Name, consumer.Method.Name, message.TriggerName);
                }
            }
        }
    }

    /// <summary>
    /// Create an instance of the consumer's declaring type via DI and invoke.
    /// </summary>
    private async Task<object?> InvokeConsumerAsync(
        TaskRegistry.ConsumerEntry consumer, object? payload, CancellationToken ct)
    {
        var instance = ActivatorUtilities.CreateInstance(_serviceProvider, consumer.DeclaringType);

        // Build arguments: if consumer expects a parameter, pass the payload
        var args = consumer.ParameterType is not null
            ? new[] { payload }
            : null;

        var taskObj = consumer.Method.Invoke(instance, args);

        if (taskObj is null)
            return null;

        var taskType = taskObj.GetType();

        if (taskType == typeof(Task))
        {
            await (Task)taskObj;
            return null;
        }

        if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            await (Task)taskObj;
            return taskType.GetProperty("Result")!.GetValue(taskObj);
        }

        return taskObj;
    }
}

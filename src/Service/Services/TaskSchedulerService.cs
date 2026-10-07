using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Library.Domain.Attributes;
using Library.Domain.Models.State;
using Library.Infrastructure.Database;
using Library.Infrastructure.MessageBus;

namespace Service.Services;

/// <summary>
/// Sleeps until the next <c>[TaskTrigger]</c> is due, invokes it,
/// and publishes the result into the <see cref="TaskChannel"/>.
/// Schedule bookmarks are persisted in the <c>AgentTasks</c> table.
/// </summary>
public sealed class TaskSchedulerService : BackgroundService
{
    private readonly ILogger<TaskSchedulerService> _logger;
    private readonly TaskRegistry _registry;
    private readonly TaskChannel _channel;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Priority queue: (triggerName, nextDueUtc). Lowest DateTime pops first.
    /// </summary>
    private readonly PriorityQueue<string, DateTime> _queue = new();

    public TaskSchedulerService(
        ILogger<TaskSchedulerService> logger,
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
        _logger.LogInformation("TaskSchedulerService starting. {Count} triggers registered.",
            _registry.Triggers.Count);

        await SeedQueueAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_queue.Count == 0)
            {
                // Nothing scheduled — sleep and re-check
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            _queue.TryPeek(out var nextName, out var nextDue);
            var delay = nextDue - DateTime.UtcNow;

            if (delay > TimeSpan.Zero)
            {
                // Sleep until the next trigger is due (or cancellation)
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            // Dequeue and fire
            if (!_queue.TryDequeue(out var triggerName, out _))
                continue;

            if (!_registry.Triggers.TryGetValue(triggerName, out var entry))
                continue;

            _logger.LogInformation("Trigger due: {Name}", triggerName);

            try
            {
                var result = await InvokeTriggerAsync(entry, stoppingToken);

                // Publish the result for any consumers
                await _channel.PublishAsync(triggerName, result, stoppingToken);

                // Bookmark
                await BookmarkAsync(triggerName, entry.DeclaringType.Name, stoppingToken);

                _logger.LogInformation("Trigger complete: {Name}", triggerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Trigger failed: {Name}. Will retry next interval.", triggerName);
            }

            // Re-enqueue at next interval
            _queue.Enqueue(triggerName, DateTime.UtcNow + entry.Interval);
        }
    }

    /// <summary>
    /// Seed the priority queue from the registry + persisted bookmarks.
    /// </summary>
    private async Task SeedQueueAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportDbContext>();
        var now = DateTime.UtcNow;

        foreach (var (name, entry) in _registry.Triggers)
        {
            var bookmark = await db.AgentTasks
                .FirstOrDefaultAsync(t => t.Name == name && t.AgentName == entry.DeclaringType.Name, ct);

            DateTime nextDue;
            if (bookmark?.LastRun is not null)
            {
                nextDue = bookmark.LastRun.Value + entry.Interval;
                if (nextDue < now) nextDue = now; // overdue — run immediately
            }
            else
            {
                nextDue = now; // never run — run immediately
            }

            _queue.Enqueue(name, nextDue);
            _logger.LogDebug("Seeded trigger '{Name}' due at {Due}", name, nextDue);
        }
    }

    /// <summary>
    /// Create an instance of the trigger's declaring type and invoke the method.
    /// Agent types need an <c>IChatClient</c>; we resolve that via the factory.
    /// </summary>
    private async Task<object?> InvokeTriggerAsync(TaskRegistry.TriggerEntry entry, CancellationToken ct)
    {
        var instance = ActivatorUtilities.CreateInstance(_serviceProvider, entry.DeclaringType);

        var arguments = entry.Method.GetParameters()
            .Select(parameter => parameter.ParameterType == typeof(CancellationToken)
                ? (object)ct
                : throw new InvalidOperationException($"Unsupported trigger parameter: {parameter.Name}"))
            .ToArray();
        var taskObj = entry.Method.Invoke(instance, arguments);

        if (taskObj is null)
            return null;

        // Await the Task and extract result if generic
        if (taskObj is Task task)
        {
            await task;

            // Check signature to see if we should extract a result
            var returnType = entry.Method.ReturnType;
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // Task<T> is completed; extract Result dynamically
                return ((dynamic)task).Result;
            }

            // Task (void)
            return null;
        }

        return taskObj;
    }

    /// <summary>Upsert the AgentTasks bookmark.</summary>
    private async Task BookmarkAsync(string triggerName, string typeName, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportDbContext>();

        var task = await db.AgentTasks
            .FirstOrDefaultAsync(t => t.Name == triggerName && t.AgentName == typeName, ct);

        if (task is null)
        {
            task = new AgentTask { Name = triggerName, AgentName = typeName };
            db.AgentTasks.Add(task);
        }

        task.LastRun = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

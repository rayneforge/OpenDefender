using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Library.Domain.Attributes;
using Microsoft.Extensions.Logging;

namespace Library.Infrastructure.MessageBus;

/// <summary>
/// Immutable registry built at startup by scanning assemblies for
/// <see cref="TaskTriggerAttribute"/> and <see cref="TaskConsumerAttribute"/> methods.
/// Validates type alignment between triggers and consumers.
/// </summary>
public sealed class TaskRegistry
{
    /// <summary>Descriptor for a <c>[TaskTrigger]</c>-decorated method.</summary>
    public sealed record TriggerEntry(
        string Name,
        string Description,
        TimeSpan Interval,
        Type DeclaringType,
        MethodInfo Method,
        Type ReturnPayloadType);

    /// <summary>Descriptor for a <c>[TaskConsumer]</c>-decorated method.</summary>
    public sealed record ConsumerEntry(
        string TriggerName,
        Type DeclaringType,
        MethodInfo Method,
        Type? ParameterType);

    private readonly Dictionary<string, TriggerEntry> _triggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<ConsumerEntry>> _consumers = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, TriggerEntry> Triggers => _triggers;
    public IReadOnlyDictionary<string, List<ConsumerEntry>> Consumers => _consumers;

    /// <summary>
    /// Scan the given types and build the registry.
    /// Call once at startup.
    /// </summary>
    public static TaskRegistry Build(IEnumerable<Type> types, ILogger? logger = null)
    {
        var registry = new TaskRegistry();

        foreach (var type in types)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                // --- Triggers ---
                var trigger = method.GetCustomAttribute<TaskTriggerAttribute>();
                if (trigger is not null)
                {
                    var returnType = UnwrapTaskResult(method.ReturnType);

                    if (registry._triggers.ContainsKey(trigger.Name))
                    {
                        logger?.LogWarning("Duplicate trigger name '{Name}' on {Type}.{Method} — skipped.",
                            trigger.Name, type.Name, method.Name);
                        continue;
                    }

                    registry._triggers[trigger.Name] = new TriggerEntry(
                        trigger.Name,
                        trigger.Description,
                        trigger.Interval,
                        type,
                        method,
                        returnType);
                }

                // --- Consumers ---
                var consumer = method.GetCustomAttribute<TaskConsumerAttribute>();
                if (consumer is not null)
                {
                    var parameters = method.GetParameters();
                    Type? paramType = parameters.Length == 1 ? parameters[0].ParameterType : null;

                    if (!registry._consumers.TryGetValue(consumer.TriggerName, out var list))
                    {
                        list = [];
                        registry._consumers[consumer.TriggerName] = list;
                    }

                    list.Add(new ConsumerEntry(
                        consumer.TriggerName,
                        type,
                        method,
                        paramType));
                }
            }
        }

        // Validate type alignment
        foreach (var (name, consumers) in registry._consumers)
        {
            if (!registry._triggers.TryGetValue(name, out var trig))
            {
                // Consumer with no trigger is fine — it can be fed via ITaskChannel
                logger?.LogInformation(
                    "Consumer(s) for '{Name}' have no [TaskTrigger] — will rely on ITaskChannel.",
                    name);
                continue;
            }

            foreach (var c in consumers)
            {
                if (c.ParameterType is not null && c.ParameterType != trig.ReturnPayloadType)
                {
                    throw new InvalidOperationException(
                        $"Type mismatch: trigger '{name}' produces {trig.ReturnPayloadType.Name} " +
                        $"but consumer {c.DeclaringType.Name}.{c.Method.Name} expects {c.ParameterType.Name}.");
                }
            }
        }

        logger?.LogInformation("TaskRegistry built: {Triggers} triggers, {Consumers} consumer groups.",
            registry._triggers.Count, registry._consumers.Count);

        return registry;
    }

    /// <summary>
    /// Unwrap <c>Task&lt;T&gt;</c> → <c>T</c>. Returns <c>typeof(void)</c> for plain <c>Task</c>.
    /// </summary>
    private static Type UnwrapTaskResult(Type returnType)
    {
        if (returnType == typeof(Task) || returnType == typeof(ValueTask))
            return typeof(void);

        if (returnType.IsGenericType)
        {
            var def = returnType.GetGenericTypeDefinition();
            if (def == typeof(Task<>) || def == typeof(ValueTask<>))
                return returnType.GetGenericArguments()[0];
        }

        return returnType;
    }
}

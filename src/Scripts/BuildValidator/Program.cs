using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Library.Domain.Attributes;

/*
 * BUILD VALIDATOR
 * This tool ensures that all Producer/Consumer triggers are consistent.
 */

try
{
    Console.WriteLine("--- Build Validation Started ---");
    
    // Scan the Library assembly which is now a project reference
    var assembly = typeof(Library.Domain.Attributes.TaskTriggerAttribute).Assembly;
    var types = assembly.GetTypes();

    var producers = new Dictionary<string, Type>();
    var consumers = new List<(string Trigger, Type Type, string Method)>();

    foreach (var type in types)
    {
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            var triggerAttr = method.GetCustomAttribute<TaskTriggerAttribute>();
            var consumerAttr = method.GetCustomAttribute<TaskConsumerAttribute>();

            if (triggerAttr != null)
            {
                Type returnType = method.ReturnType;
                if (returnType.IsGenericType && (returnType.GetGenericTypeDefinition() == typeof(Task<>) || returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
                {
                    returnType = returnType.GetGenericArguments()[0];
                }
                producers[triggerAttr.Name] = returnType;
                Console.WriteLine($"[Producer] Found '{triggerAttr.Name}' returning {returnType.Name}");
            }

            if (consumerAttr != null)
            {
                var param = method.GetParameters().FirstOrDefault();
                if (param != null)
                {
                    consumers.Add((consumerAttr.TriggerName, param.ParameterType, $"{type.Name}.{method.Name}"));
                    Console.WriteLine($"[Consumer] Found '{consumerAttr.TriggerName}' accepting {param.ParameterType.Name}");
                }
            }
        }
    }

    bool hasErrors = false;
    foreach (var consumer in consumers)
    {
        if (!producers.TryGetValue(consumer.Trigger, out var producerType))
        {
            Console.WriteLine($"[Validation Warning] Consumer '{consumer.Method}' listens to trigger '{consumer.Trigger}', but no internal producer defines it (might be external).");
            continue;
        }

        if (!consumer.Type.IsAssignableFrom(producerType))
        {
            Console.WriteLine($"[Validation Error] Type Mismatch for trigger '{consumer.Trigger}':");
            Console.WriteLine($"   - Producer offers: {producerType.Name}");
            Console.WriteLine($"   - Consumer '{consumer.Method}' requires: {consumer.Type.Name}");
            hasErrors = true;
        }
    }

    if (hasErrors)
    {
        Console.WriteLine("--- Build Validation Failed ---");
        Environment.Exit(1);
    }

    Console.WriteLine("--- Build Validation Succeeded ---");
    Environment.Exit(0);
}
catch (Exception ex)
{
    Console.WriteLine($"Build Validator Exception: {ex.Message}");
    Environment.Exit(1);
}

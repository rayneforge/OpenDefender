using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Library.Domain.Models.Evaluation;

namespace Library.Domain.Abstractions;

/// <summary>
/// A lightweight wrapper for an AI agent.
/// Encapsulates the chat client and provides a simplified execution interface.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Executes the agent with the given messages and tools, returning a string response.
    /// </summary>
    Task<AgentRunResponse<string>> RunAsync(
        IEnumerable<ChatMessage> messages, 
        IEnumerable<AITool>? tools = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the agent with the given messages and tools, returning a structured response of type T.
    /// </summary>
    Task<AgentRunResponse<T>> RunAsync<T>(
        IEnumerable<ChatMessage> messages, 
        IEnumerable<AITool>? tools = null, 
        CancellationToken cancellationToken = default);
}

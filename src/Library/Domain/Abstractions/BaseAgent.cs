using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Library.Domain.Abstractions;
using Library.Domain.Models.Evaluation;

namespace Library.Domain.Abstractions;

/// <summary>
/// Base class for delegated agents handling common execution logic.
/// </summary>
public abstract class BaseAgent : IAgent
{
    private readonly IChatClient _client;

    protected abstract string SystemPrompt { get; }
    protected abstract IEnumerable<AITool> DefaultTools { get; }

    protected BaseAgent(IChatClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<AgentRunResponse<string>> RunAsync(
        IEnumerable<ChatMessage> messages, 
        IEnumerable<AITool>? tools = null, 
        CancellationToken cancellationToken = default)
    {
        var chatMessages = PrepareMessages(messages);
        var options = new ChatOptions
        {
            Tools = tools?.ToList() ?? DefaultTools.ToList()
        };

        var response = await _client.GetResponseAsync(chatMessages, options, cancellationToken);
        return AgentRunResponse<string>.FromText(response.Text ?? string.Empty, chatMessages);
    }




    public async Task<AgentRunResponse<T>> RunAsync<T>(
        IEnumerable<ChatMessage> messages, 
        IEnumerable<AITool>? tools = null, 
        CancellationToken cancellationToken = default)
    {
        var chatMessages = PrepareMessages(messages);
        var options = new ChatOptions
        {
            Tools = tools?.ToList() ?? DefaultTools.ToList()
        };

        var response = await _client.GetResponseAsync<T>(chatMessages, options, cancellationToken: cancellationToken);
        
        return AgentRunResponse<T>.FromValue(response.Result, chatMessages);
    }

    private List<ChatMessage> PrepareMessages(IEnumerable<ChatMessage> messages)
    {
        var chatMessages = new List<ChatMessage> { new ChatMessage(ChatRole.System, SystemPrompt) };
        chatMessages.AddRange(messages);
        return chatMessages;
    }
}

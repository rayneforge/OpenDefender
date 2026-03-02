using Microsoft.Extensions.AI;
using OllamaSharp;

namespace Library.Application.Factories;

/// <summary>
/// Single swap-point for the underlying LLM provider.
/// Every test creates its pipeline through here.
/// </summary>
/// <remarks>
/// Pattern from the official Microsoft docs (learn.microsoft.com/dotnet/ai/ichatclient#tool-calling):
///   new ChatClientBuilder(innerClient).UseFunctionInvocation().Build()
/// The FunctionInvokingChatClient middleware IS the agent loop — it intercepts
/// tool-call requests from the model, executes each AIFunction, and feeds results
/// back until the model produces a final text response.
/// </remarks>
public static class ChatClientFactory
{
    private static readonly Uri OllamaEndpoint =
        new(Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434");

    private static readonly string Model =
        Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "gpt-oss:120b-cloud";

    /// <summary>
    /// Creates an <see cref="IChatClient"/> with the
    /// <see cref="FunctionInvokingChatClient"/> middleware.
    /// Pass MCP tools via <see cref="ChatOptions.Tools"/> — they will be
    /// executed automatically when the model requests them.
    /// </summary>
    public static IChatClient Create()
    {
        return new ChatClientBuilder(new OllamaApiClient(OllamaEndpoint, Model))
            .UseFunctionInvocation()
            .Build();
    }
}

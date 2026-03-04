using System.ClientModel;
using Azure.AI.OpenAI;
using Library.Domain.Models.State;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace Library.Application.Factories;

/// <summary>
/// Single swap-point for the underlying LLM provider.
/// Every agent creates its pipeline through here.
/// </summary>
/// <remarks>
/// Supported providers (controlled via <see cref="LlmOptions.Provider"/>):
/// <list type="bullet">
///   <item><description><b>Ollama</b> — local inference via OllamaSharp.</description></item>
///   <item><description><b>OpenAI</b> — OpenAI platform via <c>Microsoft.Extensions.AI.OpenAI</c>.</description></item>
///   <item><description><b>AzureOpenAI</b> — Azure-hosted models via <c>Azure.AI.OpenAI</c>.</description></item>
/// </list>
///
/// The <see cref="FunctionInvokingChatClient"/> middleware IS the agent loop — it intercepts
/// tool-call requests from the model, executes each <see cref="AIFunction"/>, and feeds results
/// back until the model produces a final text response.
/// </remarks>
public static class ChatClientFactory
{
    /// <summary>
    /// Creates an <see cref="IChatClient"/> using default environment variables.
    /// Preserves backward compatibility for tests.
    /// </summary>
    public static IChatClient Create()
    {
        var options = new LlmOptions();

        var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT");
        if (!string.IsNullOrEmpty(endpoint)) options.Ollama.Endpoint = endpoint;

        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL");
        if (!string.IsNullOrEmpty(model)) options.Ollama.Model = model;

        return Create(options);
    }

    /// <summary>
    /// Creates an <see cref="IChatClient"/> backed by the provider specified in <paramref name="options"/>,
    /// wrapped with the <see cref="FunctionInvokingChatClient"/> middleware.
    /// Pass MCP tools via <see cref="ChatOptions.Tools"/> — they will be
    /// executed automatically when the model requests them.
    /// </summary>
    public static IChatClient Create(LlmOptions options)
    {
        IChatClient innerClient = options.Provider switch
        {
            "OpenAI" => CreateOpenAiClient(options.OpenAi),
            "AzureOpenAI" => CreateAzureOpenAiClient(options.AzureOpenAi),
            _ => CreateOllamaClient(options.Ollama),
        };

        return new ChatClientBuilder(innerClient)
            .UseFunctionInvocation()
            .Build();
    }

    private static IChatClient CreateOllamaClient(OllamaOptions opts)
    {
        return new OllamaApiClient(new Uri(opts.Endpoint), opts.Model);
    }

    private static IChatClient CreateOpenAiClient(OpenAiOptions opts)
    {
        // OpenAI.Chat.ChatClient → .AsIChatClient() extension from Microsoft.Extensions.AI.OpenAI
        return new OpenAI.Chat.ChatClient(opts.Model, new ApiKeyCredential(opts.ApiKey))
            .AsIChatClient();
    }

    private static IChatClient CreateAzureOpenAiClient(AzureOpenAiOptions opts)
    {
        AzureOpenAIClient azureClient = opts.AuthenticationStrategy switch
        {
            // Use DefaultAzureCredential for managed identity / az login / env-based auth
            "ManagedIdentity" => new AzureOpenAIClient(
                new Uri(opts.Endpoint),
                new Azure.Identity.DefaultAzureCredential()),

            // Default: API key authentication
            _ => new AzureOpenAIClient(
                new Uri(opts.Endpoint),
                new ApiKeyCredential(opts.ApiKey)),
        };

        return azureClient
            .GetChatClient(opts.DeploymentName)
            .AsIChatClient();
    }
}

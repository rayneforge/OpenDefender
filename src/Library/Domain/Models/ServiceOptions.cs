namespace Library.Domain.Models.State;

public class ServiceOptions
{
    public const string SectionName = "Service";

    /// <summary>
    /// Only supported communication mode: "Stdio". Other values are rejected.
    /// </summary>
    public string TransportType { get; set; } = "Stdio";

    /// <summary>Opt in to packet capture using only permissions already held by the process.</summary>
    public bool EnablePacketCapture { get; set; }

    /// <summary>
    /// Frequency of background diagnostic collection (e.g. "00:05:00" for 5 minutes)
    /// </summary>
    public TimeSpan CollectionFrequency { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Connection string for the SQLite diagnostic database
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=diagnostic_reports.db";

    /// <summary>
    /// Historical window for the orchestrator (defaults to 24 hours)
    /// </summary>
    public int LookbackHours { get; set; } = 24;

    /// <summary>
    /// Number of minutes to retain data before the retention service purges it.
    /// </summary>
    public int RetentionMinutes { get; set; } = 120;

    /// <summary>
    /// How often the retention service runs (e.g. "00:15:00" for every 15 minutes).
    /// </summary>
    public TimeSpan RetentionFrequency { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// How often the delegated agents run their scheduled checks (e.g. "01:00:00" for every hour).
    /// </summary>
    public TimeSpan AgentRunInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// LLM Provider Configuration.
    /// When <c>null</c> (i.e. the <c>Llm</c> section is absent from config),
    /// all agent controllers, triggers, and the IChatClient registration are skipped.
    /// </summary>
    public LlmOptions? Llm { get; set; }
}

/// <summary>
/// Configuration for the underlying LLM provider.
/// </summary>
public class LlmOptions
{
    /// <summary>
    /// The provider to use: "Ollama", "OpenAI", or "AzureOpenAI".
    /// </summary>
    public string Provider { get; set; } = "Ollama";

    public OllamaOptions Ollama { get; set; } = new();
    public OpenAiOptions OpenAi { get; set; } = new();
    public AzureOpenAiOptions AzureOpenAi { get; set; } = new();
}

public class OllamaOptions
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "gpt-oss:120b-cloud";
}

public class OpenAiOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o";
}

public class AzureOpenAiOptions
{
    public string Endpoint { get; set; } = "";
    public string DeploymentName { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string AuthenticationStrategy { get; set; } = "ApiKey";
}

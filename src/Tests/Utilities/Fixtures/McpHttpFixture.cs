using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Azure.Identity;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Client;

namespace Tests.Utilities.Fixtures;

/// <summary>
/// Connects an MCP client to any HTTP MCP endpoint with pluggable auth.
/// All configuration is via environment variables — no secrets in code.
///
/// <b>Transport:</b>
/// <list type="table">
///   <item><term>MCP_HTTP_ENDPOINT</term><description>MCP server URL (default: http://localhost:5000/mcp)</description></item>
///   <item><term>MCP_HTTP_NAME</term><description>Friendly transport name (default: derived from host)</description></item>
/// </list>
///
/// <b>Auth (MCP_AUTH_TYPE):</b>
/// <list type="table">
///   <item><term>none</term><description>(default) No authentication</description></item>
///   <item><term>apikey</term><description>Sends MCP_API_KEY as a Bearer token (or custom header via MCP_API_KEY_HEADER)</description></item>
///   <item><term>oauth</term><description>SDK-native OAuth with auto-probe. On startup, probes the endpoint's discovery chain
///     (RFC 9728 → RFC 8414) to check for a <c>registration_endpoint</c>. If found, uses dynamic
///     registration (RFC 7591). If not, requires OAUTH_CLIENT_ID for a pre-registered client.
///     Authorization Code + PKCE via loopback browser redirect.
///     Reads OAUTH_CLIENT_ID, OAUTH_CLIENT_SECRET, OAUTH_SCOPES, OAUTH_REDIRECT_URI.</description></item>
///   <item><term>entra_default</term><description>Azure <see cref="DefaultAzureCredential"/> → Bearer token.
///     Reads MCP_ENTRA_SCOPE (required), MCP_ENTRA_TENANT_ID (optional).</description></item>
/// </list>
/// </summary>
public sealed class McpHttpFixture : IAsyncLifetime
{
    private McpClient? _client;
    private IList<McpClientTool>? _tools;

    public McpClient Client => _client ?? throw new InvalidOperationException("Fixture not initialized.");
    public IList<McpClientTool> Tools => _tools ?? throw new InvalidOperationException("Fixture not initialized.");

    public async Task InitializeAsync()
    {
        var endpoint = new Uri(
            Environment.GetEnvironmentVariable("MCP_HTTP_ENDPOINT")
            ?? "http://localhost:5000/mcp");

        var name = Environment.GetEnvironmentVariable("MCP_HTTP_NAME")
            ?? endpoint.Host;

        var options = new HttpClientTransportOptions
        {
            Name = name,
            Endpoint = endpoint,
        };

        var authType = (Environment.GetEnvironmentVariable("MCP_AUTH_TYPE") ?? "none")
            .ToLowerInvariant();

        switch (authType)
        {
            case "apikey":
                ApplyApiKeyAuth(options);
                break;
            case "oauth":
                await ApplyOAuthAsync(options);
                break;
            case "entra_default":
                await ApplyEntraDefaultAsync(options);
                break;
            // "none" — nothing to configure
        }

        var transport = new HttpClientTransport(options);
        _client = await McpClient.CreateAsync(transport);
        _tools = await _client.ListToolsAsync();
    }

    public async Task DisposeAsync()
    {
        if (_client is IAsyncDisposable d) await d.DisposeAsync();
    }

    /// <summary>
    /// Reads MCP_API_KEY and injects it as a Bearer token (default) or a custom header.
    /// Set MCP_API_KEY_HEADER to override the header name (e.g. "x-api-key").
    /// </summary>
    private static void ApplyApiKeyAuth(HttpClientTransportOptions options)
    {
        var key = Environment.GetEnvironmentVariable("MCP_API_KEY")
            ?? throw new InvalidOperationException("MCP_AUTH_TYPE=apikey requires MCP_API_KEY.");

        var header = Environment.GetEnvironmentVariable("MCP_API_KEY_HEADER");

        options.AdditionalHeaders ??= new Dictionary<string, string>();

        if (string.IsNullOrEmpty(header))
        {
            // Default: Bearer token in Authorization header
            options.AdditionalHeaders["Authorization"] = $"Bearer {key}";
        }
        else
        {
            options.AdditionalHeaders[header] = key;
        }
    }

    /// <summary>
    /// Uses <see cref="DefaultAzureCredential"/> to obtain a Bearer token for the given scope.
    /// Reads MCP_ENTRA_SCOPE (required) and optionally MCP_ENTRA_TENANT_ID.
    /// <see cref="DefaultAzureCredential"/> chains through: environment variables, managed identity,
    /// Visual Studio, Azure CLI, Azure PowerShell, Azure Developer CLI, and interactive browser.
    /// </summary>
    private static async Task ApplyEntraDefaultAsync(HttpClientTransportOptions options)
    {
        var scope = Environment.GetEnvironmentVariable("MCP_ENTRA_SCOPE")
            ?? throw new InvalidOperationException("MCP_AUTH_TYPE=entra_default requires MCP_ENTRA_SCOPE (e.g. api://my-app/.default).");

        var tenantId = Environment.GetEnvironmentVariable("MCP_ENTRA_TENANT_ID");

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            TenantId = tenantId,
        });

        var token = await credential.GetTokenAsync(
            new Azure.Core.TokenRequestContext([scope]));

        options.AdditionalHeaders ??= new Dictionary<string, string>();
        options.AdditionalHeaders["Authorization"] = $"Bearer {token.Token}";
    }

    // ───────────────────────── OAuth ─────────────────────────

    /// <summary>
    /// Single OAuth entry point with auto-probe. Probes the MCP endpoint's discovery chain
    /// to determine whether the authorization server supports dynamic client registration:
    /// <list type="number">
    ///   <item>Sends a warm-up POST to the endpoint → expects HTTP 401</item>
    ///   <item>Parses <c>resource_metadata</c> from <c>WWW-Authenticate</c> (RFC 9728)</item>
    ///   <item>Fetches Protected Resource Metadata → <c>authorization_servers</c></item>
    ///   <item>Fetches Authorization Server Metadata (RFC 8414)</item>
    ///   <item>If <c>registration_endpoint</c> exists → dynamic registration (no client ID needed)</item>
    ///   <item>If not → requires <c>OAUTH_CLIENT_ID</c> for a pre-registered OAuth App</item>
    /// </list>
    /// Authorization Code + PKCE with loopback browser redirect in both paths.
    /// </summary>
    private async Task ApplyOAuthAsync(HttpClientTransportOptions options)
    {
        var oauthOptions = BuildOAuthOptions();
        var supportsDynamic = await ProbeForDynamicRegistrationAsync(options.Endpoint);

        if (supportsDynamic)
        {
            Console.Error.WriteLine("[McpHttpFixture] OAuth probe: dynamic registration supported");
        }
        else
        {
            Console.Error.WriteLine("[McpHttpFixture] OAuth probe: no dynamic registration — requiring OAUTH_CLIENT_ID");
            var clientId = Environment.GetEnvironmentVariable("OAUTH_CLIENT_ID")
                ?? throw new InvalidOperationException(
                    "MCP_AUTH_TYPE=oauth: The authorization server does not support dynamic client registration. "
                    + "Set OAUTH_CLIENT_ID to a pre-registered OAuth App client ID.");
            oauthOptions.ClientId = clientId;
        }

        var clientSecret = Environment.GetEnvironmentVariable("OAUTH_CLIENT_SECRET");
        if (!string.IsNullOrEmpty(clientSecret))
            oauthOptions.ClientSecret = clientSecret;

        options.OAuth = oauthOptions;
    }

    /// <summary>
    /// Probes the MCP endpoint's full OAuth discovery chain to determine whether the
    /// authorization server supports dynamic client registration (RFC 7591).
    /// <para>
    /// Discovery chain: endpoint → 401 WWW-Authenticate → resource_metadata (RFC 9728)
    /// → authorization_servers → AS metadata (RFC 8414) → registration_endpoint?
    /// </para>
    /// </summary>
    private static async Task<bool> ProbeForDynamicRegistrationAsync(Uri endpoint)
    {
        using var http = new HttpClient();

        try
        {
            // Step 1: Warm-up request → expect 401 with WWW-Authenticate header
            var warmup = await http.SendAsync(new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
            });

            if (warmup.StatusCode != HttpStatusCode.Unauthorized)
                return false; // No auth required or unexpected response

            var wwwAuth = warmup.Headers.WwwAuthenticate.ToString();

            // Step 2: Extract resource_metadata URL from WWW-Authenticate
            var resourceMetadataUrl = ExtractParameter(wwwAuth, "resource_metadata");
            if (resourceMetadataUrl is null)
                return false;

            // Step 3: Fetch Protected Resource Metadata (RFC 9728)
            var resourceJson = await http.GetStringAsync(resourceMetadataUrl);
            using var resourceDoc = JsonDocument.Parse(resourceJson);
            var root = resourceDoc.RootElement;

            if (!root.TryGetProperty("authorization_servers", out var servers) || servers.GetArrayLength() == 0)
                return false;

            var authServer = servers[0].GetString();
            if (authServer is null)
                return false;

            // Step 4: Fetch Authorization Server Metadata (RFC 8414)
            // The AS metadata URL is {origin}/.well-known/oauth-authorization-server{path}
            var authServerUri = new Uri(authServer);
            var metadataUrl = $"{authServerUri.Scheme}://{authServerUri.Host}/.well-known/oauth-authorization-server{authServerUri.AbsolutePath}";

            var asJson = await http.GetStringAsync(metadataUrl);
            using var asDoc = JsonDocument.Parse(asJson);

            // Step 5: Check for registration_endpoint
            return asDoc.RootElement.TryGetProperty("registration_endpoint", out _);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[McpHttpFixture] Probe failed, defaulting to oauth_standard: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Extracts a named parameter value from a WWW-Authenticate header value.
    /// Handles both quoted and unquoted values.
    /// Example: <c>Bearer error="invalid_request", resource_metadata="https://..."</c>
    /// </summary>
    private static string? ExtractParameter(string header, string paramName)
    {
        var key = $"{paramName}=";
        var idx = header.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        var start = idx + key.Length;
        if (start >= header.Length) return null;

        if (header[start] == '"')
        {
            var end = header.IndexOf('"', start + 1);
            return end < 0 ? null : header[(start + 1)..end];
        }
        else
        {
            var end = header.IndexOfAny([',', ' '], start);
            return end < 0 ? header[start..] : header[start..end];
        }
    }

    /// <summary>
    /// Builds shared <see cref="ClientOAuthOptions"/> — redirect URI, loopback delegate, and
    /// optional scopes. Client ID is set by the caller based on probe results.
    /// </summary>
    private static ClientOAuthOptions BuildOAuthOptions()
    {
        var redirectUri = Environment.GetEnvironmentVariable("OAUTH_REDIRECT_URI")
            ?? "http://localhost:8900/";

        if (!redirectUri.EndsWith('/'))
            redirectUri += "/";

        var oauthOptions = new ClientOAuthOptions
        {
            RedirectUri = new Uri(redirectUri),
            AuthorizationRedirectDelegate = CreateLoopbackRedirectDelegate(),
        };

        var scopes = Environment.GetEnvironmentVariable("OAUTH_SCOPES");
        if (!string.IsNullOrEmpty(scopes))
            oauthOptions.Scopes = scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return oauthOptions;
    }

    /// <summary>
    /// Creates an <see cref="AuthorizationRedirectDelegate"/> that:
    /// <list type="number">
    ///   <item>Starts a temporary <see cref="HttpListener"/> on the redirect URI</item>
    ///   <item>Opens the authorization URL in the system default browser</item>
    ///   <item>Waits for the OAuth callback, extracts the <c>code</c> query parameter</item>
    ///   <item>Returns a success page to the browser and shuts down the listener</item>
    /// </list>
    /// This is the standard loopback redirect flow for CLI/desktop OAuth clients.
    /// </summary>
    private static AuthorizationRedirectDelegate CreateLoopbackRedirectDelegate()
    {
        return async (Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken) =>
        {
            // Ensure the prefix ends with / for HttpListener
            var prefix = redirectUri.ToString();
            if (!prefix.EndsWith('/'))
                prefix += "/";

            using var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            try
            {
                // Open the authorization URL in the system browser
                OpenBrowser(authorizationUri.ToString());

                // Wait for the OAuth callback
                var context = await listener.GetContextAsync()
                    .WaitAsync(cancellationToken);

                var query = context.Request.Url?.Query;
                var parameters = HttpUtility.ParseQueryString(query ?? string.Empty);

                var code = parameters["code"];
                var error = parameters["error"];

                // Send a response page to the browser
                var responseHtml = code is not null
                    ? "<html><body><h2>Authorization successful.</h2><p>You can close this tab.</p></body></html>"
                    : $"<html><body><h2>Authorization failed.</h2><p>{error ?? "No code received."}</p></body></html>";

                var buffer = System.Text.Encoding.UTF8.GetBytes(responseHtml);
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer, cancellationToken);
                context.Response.Close();

                if (error is not null)
                    throw new InvalidOperationException($"OAuth authorization failed: {error} — {parameters["error_description"]}");

                return code;
            }
            finally
            {
                listener.Stop();
            }
        };
    }

    /// <summary>
    /// Opens a URL in the system default browser across Linux, macOS, and Windows.
    /// </summary>
    private static void OpenBrowser(string url)
    {
        try
        {
            if (OperatingSystem.IsLinux())
                Process.Start(new ProcessStartInfo("xdg-open", url) { UseShellExecute = false });
            else if (OperatingSystem.IsMacOS())
                Process.Start(new ProcessStartInfo("open", url) { UseShellExecute = false });
            else if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else
                throw new PlatformNotSupportedException("Cannot open browser on this platform.");
        }
        catch (Exception ex)
        {
            // If browser launch fails, fall back to printing the URL for manual use
            Console.Error.WriteLine($"[McpHttpFixture] Could not open browser: {ex.Message}");
            Console.Error.WriteLine($"[McpHttpFixture] Open this URL manually: {url}");
        }
    }
}

[CollectionDefinition(McpHttpCollection.Name)]
public class McpHttpCollection : ICollectionFixture<McpHttpFixture>
{
    public const string Name = "McpHttp";
}

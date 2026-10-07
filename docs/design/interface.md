# Client interface

OpenDefender exposes local MCP tools and prompts over stdio only. The MCP client starts the executable and presents tool results in its own interface.

There is no HTTP host, OData API, browser dashboard, or remote MCP endpoint. Legacy HTTP configuration is rejected. To inspect current TCP peers, call `query_network_connections`; to review stored metrics, use the corresponding query tools with bounded paging.

Tool output may reveal service names, log paths, system configuration, and network endpoints. The client controls whether these results are sent to an AI provider or retained in conversation history. Live TCP snapshots are not saved in OpenDefender's SQLite databases.

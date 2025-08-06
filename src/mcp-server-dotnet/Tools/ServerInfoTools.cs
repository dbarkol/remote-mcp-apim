using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpServerDotnet.Tools;

[McpServerToolType]
public class ServerInfoTools
{
    private readonly ILogger<ServerInfoTools> _logger;

    public ServerInfoTools(ILogger<ServerInfoTools> logger)
    {
        _logger = logger;
    }

    [McpServerTool, Description("Get information about the .NET MCP server")]
    public async Task<string> GetServerInfo(
        [Description("Include environment information")] bool includeEnvironment = false)
    {
        _logger.LogInformation("GetServerInfo tool called with includeEnvironment: {IncludeEnvironment}", includeEnvironment);

        var serverInfo = new
        {
            ServerName = "MCP Server (.NET)",
            Version = "2.0.0",
            Runtime = "Azure Functions",
            Protocol = "Model Context Protocol",
            ProtocolVersion = "2024-11-05",
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Environment = includeEnvironment ? new
            {
                DotNetVersion = Environment.Version.ToString(),
                OSVersion = Environment.OSVersion.ToString(),
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                WorkingSet = Environment.WorkingSet,
                IsContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
            } : null
        };

        return await Task.FromResult(System.Text.Json.JsonSerializer.Serialize(serverInfo, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        }));
    }

    [McpServerTool, Description("Get the current server status and health information")]
    public async Task<string> GetServerStatus()
    {
        _logger.LogInformation("GetServerStatus tool called");

        var status = new
        {
            Status = "healthy",
            Service = "MCP Server (.NET)",
            Version = "2.0.0",
            Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(),
            MemoryUsage = new
            {
                WorkingSet = Environment.WorkingSet,
                GCTotalMemory = GC.GetTotalMemory(false)
            },
            LastCheck = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        return await Task.FromResult(System.Text.Json.JsonSerializer.Serialize(status, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        }));
    }

    [McpServerTool, Description("Echo a message back to the client")]
    public async Task<string> Echo(
        [Description("The message to echo back")] string message)
    {
        _logger.LogInformation("Echo tool called with message: {Message}", message);
        return await Task.FromResult($"Echo: {message}");
    }
}

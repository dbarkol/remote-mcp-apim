using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServerDotnet;

public class McpFunctions
{
    private readonly ILogger<McpFunctions> _logger;

    public McpFunctions(ILogger<McpFunctions> logger)
    {
        _logger = logger;
    }

    [Function("HealthCheck")]
    public IActionResult HealthCheck([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "")] HttpRequest req)
    {
        _logger.LogInformation("Health check requested");
        
        return new OkObjectResult(new
        {
            status = "healthy",
            service = "MCP Server (.NET)",
            version = "1.0.0"
        });
    }

    [Function("HandleMcp")]
    public async Task<IActionResult> HandleMcp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mcp")] HttpRequest req)
    {
        _logger.LogInformation("MCP request received");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var mcpRequest = JsonSerializer.Deserialize<McpRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (mcpRequest == null)
            {
                return new BadRequestObjectResult("Invalid request");
            }

            var response = mcpRequest.Method switch
            {
                "initialize" => HandleInitialize(mcpRequest),
                "tools/list" => HandleToolsList(mcpRequest),
                "tools/call" => HandleToolsCall(mcpRequest),
                "resources/list" => HandleResourcesList(mcpRequest),
                "resources/read" => HandleResourcesRead(mcpRequest),
                _ => new McpResponse
                {
                    Id = mcpRequest.Id,
                    Error = new McpError
                    {
                        Code = -32601,
                        Message = $"Method not found: {mcpRequest.Method}"
                    }
                }
            };

            return new OkObjectResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request");
            return new ObjectResult(new McpResponse
            {
                Error = new McpError
                {
                    Code = -32603,
                    Message = $"Internal error: {ex.Message}"
                }
            })
            { StatusCode = 500 };
        }
    }

    private McpResponse HandleInitialize(McpRequest request)
    {
        return new McpResponse
        {
            Id = request.Id,
            Result = new Dictionary<string, object>
            {
                ["protocolVersion"] = "2024-11-05",
                ["capabilities"] = new Dictionary<string, object>
                {
                    ["tools"] = new Dictionary<string, object> { ["listChanged"] = true },
                    ["resources"] = new Dictionary<string, object> 
                    { 
                        ["subscribe"] = true, 
                        ["listChanged"] = true 
                    }
                },
                ["serverInfo"] = new Dictionary<string, object>
                {
                    ["name"] = "mcp-server-dotnet",
                    ["version"] = "1.0.0"
                }
            }
        };
    }

    private McpResponse HandleToolsList(McpRequest request)
    {
        var tools = new[]
        {
            new
            {
                name = "get_server_info",
                description = "Get information about the .NET MCP server",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        includeEnvironment = new
                        {
                            type = "boolean",
                            description = "Include environment information",
                            @default = false
                        }
                    }
                }
            }
        };

        return new McpResponse
        {
            Id = request.Id,
            Result = new Dictionary<string, object>
            {
                ["tools"] = tools
            }
        };
    }

    private McpResponse HandleToolsCall(McpRequest request)
    {
        var toolName = request.Params?.GetValueOrDefault("name")?.ToString();
        var arguments = request.Params?.GetValueOrDefault("arguments") as JsonElement?;

        if (toolName == "get_server_info")
        {
            var includeEnvironment = false;
            if (arguments?.TryGetProperty("includeEnvironment", out var envProp) == true)
            {
                includeEnvironment = envProp.GetBoolean();
            }

            var serverInfo = new Dictionary<string, object>
            {
                ["serverName"] = "MCP Server (.NET)",
                ["version"] = "1.0.0",
                ["runtime"] = "Azure Functions",
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            if (includeEnvironment)
            {
                serverInfo["environment"] = new Dictionary<string, object>
                {
                    ["dotnetVersion"] = Environment.Version.ToString(),
                    ["osVersion"] = Environment.OSVersion.ToString(),
                    ["machineName"] = Environment.MachineName
                };
            }

            return new McpResponse
            {
                Id = request.Id,
                Result = new Dictionary<string, object>
                {
                    ["content"] = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Server Information: {JsonSerializer.Serialize(serverInfo)}"
                        }
                    }
                }
            };
        }

        return new McpResponse
        {
            Id = request.Id,
            Error = new McpError
            {
                Code = -32601,
                Message = $"Unknown tool: {toolName}"
            }
        };
    }

    private McpResponse HandleResourcesList(McpRequest request)
    {
        var resources = new[]
        {
            new
            {
                uri = "dotnet://server-config",
                name = "Server Configuration",
                description = "Configuration information for the .NET MCP server",
                mimeType = "application/json"
            }
        };

        return new McpResponse
        {
            Id = request.Id,
            Result = new Dictionary<string, object>
            {
                ["resources"] = resources
            }
        };
    }

    private McpResponse HandleResourcesRead(McpRequest request)
    {
        var uri = request.Params?.GetValueOrDefault("uri")?.ToString();

        if (uri == "dotnet://server-config")
        {
            var config = new
            {
                serverName = "MCP Server (.NET)",
                version = "1.0.0",
                capabilities = new[] { "tools", "resources" },
                configuration = new
                {
                    runtime = "Azure Functions",
                    language = ".NET 8.0"
                }
            };

            return new McpResponse
            {
                Id = request.Id,
                Result = new Dictionary<string, object>
                {
                    ["contents"] = new[]
                    {
                        new
                        {
                            uri = uri,
                            mimeType = "application/json",
                            text = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true })
                        }
                    }
                }
            };
        }

        return new McpResponse
        {
            Id = request.Id,
            Error = new McpError
            {
                Code = -32602,
                Message = $"Resource not found: {uri}"
            }
        };
    }
}

public class McpRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public Dictionary<string, object>? Params { get; set; }
}

public class McpResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("result")]
    public Dictionary<string, object>? Result { get; set; }

    [JsonPropertyName("error")]
    public McpError? Error { get; set; }
}

public class McpError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

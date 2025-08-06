using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using System.Text.Json;
using McpServerDotnet.Tools;
using System.Reflection;

namespace McpServerDotnet;

public class McpFunctions
{
    private readonly ILogger<McpFunctions> _logger;
    private readonly ServerInfoTools _serverInfoTools;

    public McpFunctions(ILogger<McpFunctions> logger, ServerInfoTools serverInfoTools)
    {
        _logger = logger;
        _serverInfoTools = serverInfoTools;
    }

    [Function("HealthCheck")]
    public IActionResult HealthCheck([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "")] HttpRequest req)
    {
        _logger.LogInformation("Health check requested");
        
        return new OkObjectResult(new
        {
            status = "healthy",
            service = "MCP Server (.NET)",
            version = "2.0.0",
            protocol = "Model Context Protocol",
            protocolVersion = "2024-11-05",
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        });
    }

    [Function("HandleMcp")]
    public async Task<IActionResult> HandleMcp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mcp")] HttpRequest req)
    {
        _logger.LogInformation("MCP request received");

        try
        {
            using var reader = new StreamReader(req.Body);
            var requestBody = await reader.ReadToEndAsync();
            
            _logger.LogDebug("MCP request body: {RequestBody}", requestBody);

            // Parse the JSON-RPC request
            var jsonDocument = JsonDocument.Parse(requestBody);
            var root = jsonDocument.RootElement;

            if (!root.TryGetProperty("method", out var methodElement))
            {
                return new BadRequestObjectResult(new
                {
                    jsonrpc = "2.0",
                    error = new { code = -32600, message = "Invalid Request" },
                    id = root.TryGetProperty("id", out var idElement) ? GetIdValue(idElement) : null
                });
            }

            var method = methodElement.GetString();
            var id = root.TryGetProperty("id", out var requestIdElement) ? GetIdValue(requestIdElement) : null;

            _logger.LogInformation("Processing MCP method: {Method}", method);

            // Handle the MCP request based on method
            var response = method switch
            {
                "initialize" => HandleInitialize(root, id),
                "tools/list" => HandleToolsList(root, id),
                "tools/call" => await HandleToolsCall(root, id),
                "resources/list" => HandleResourcesList(root, id),
                "resources/read" => HandleResourcesRead(root, id),
                _ => CreateErrorResponse(id, -32601, $"Method not found: {method}")
            };

            return new OkObjectResult(response);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error in MCP request");
            return new BadRequestObjectResult(new
            {
                jsonrpc = "2.0",
                error = new { code = -32700, message = "Parse error" },
                id = (object?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request");
            return new ObjectResult(new
            {
                jsonrpc = "2.0",
                error = new { code = -32603, message = $"Internal error: {ex.Message}" },
                id = (object?)null
            })
            { StatusCode = 500 };
        }
    }

    private static object? GetIdValue(JsonElement idElement)
    {
        return idElement.ValueKind switch
        {
            JsonValueKind.String => idElement.GetString(),
            JsonValueKind.Number => idElement.TryGetInt32(out var intVal) ? intVal : idElement.GetDouble(),
            JsonValueKind.Null => null,
            _ => idElement.ToString()
        };
    }

    private object HandleInitialize(JsonElement request, object? id)
    {
        _logger.LogInformation("Handling initialize request");

        var serverInfo = new Implementation 
        { 
            Name = "mcp-server-dotnet", 
            Version = "2.0.0" 
        };

        return new
        {
            jsonrpc = "2.0",
            id = id,
            result = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new
                {
                    tools = new { listChanged = true },
                    resources = new { subscribe = true, listChanged = true }
                },
                serverInfo = new
                {
                    name = serverInfo.Name,
                    version = serverInfo.Version
                }
            }
        };
    }

    private object HandleToolsList(JsonElement request, object? id)
    {
        _logger.LogInformation("Handling tools/list request");

        // Get available tools using reflection on ServerInfoTools
        var tools = GetAvailableTools();

        return new
        {
            jsonrpc = "2.0",
            id = id,
            result = new
            {
                tools = tools.ToArray()
            }
        };
    }

    private async Task<object> HandleToolsCall(JsonElement request, object? id)
    {
        _logger.LogInformation("Handling tools/call request");

        try
        {
            if (!request.TryGetProperty("params", out var paramsElement))
            {
                return CreateErrorResponse(id, -32602, "Missing params");
            }

            if (!paramsElement.TryGetProperty("name", out var nameElement))
            {
                return CreateErrorResponse(id, -32602, "Missing tool name");
            }

            var toolName = nameElement.GetString();
            var arguments = new Dictionary<string, object?>();

            if (paramsElement.TryGetProperty("arguments", out var argsElement))
            {
                foreach (var property in argsElement.EnumerateObject())
                {
                    arguments[property.Name] = GetJsonElementValue(property.Value);
                }
            }

            _logger.LogInformation("Calling tool: {ToolName} with arguments: {Arguments}", 
                toolName, JsonSerializer.Serialize(arguments));

            // Call the appropriate tool method
            var result = await CallTool(toolName, arguments);

            return new
            {
                jsonrpc = "2.0",
                id = id,
                result = new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = result
                        }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling tool");
            return CreateErrorResponse(id, -32603, $"Error calling tool: {ex.Message}");
        }
    }

    private object HandleResourcesList(JsonElement request, object? id)
    {
        _logger.LogInformation("Handling resources/list request");

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

        return new
        {
            jsonrpc = "2.0",
            id = id,
            result = new { resources }
        };
    }

    private object HandleResourcesRead(JsonElement request, object? id)
    {
        _logger.LogInformation("Handling resources/read request");

        if (!request.TryGetProperty("params", out var paramsElement) ||
            !paramsElement.TryGetProperty("uri", out var uriElement))
        {
            return CreateErrorResponse(id, -32602, "Missing URI parameter");
        }

        var uri = uriElement.GetString();

        if (uri == "dotnet://server-config")
        {
            var config = new
            {
                serverName = "MCP Server (.NET)",
                version = "2.0.0",
                protocol = "Model Context Protocol",
                protocolVersion = "2024-11-05",
                capabilities = new[] { "tools", "resources" },
                configuration = new
                {
                    runtime = "Azure Functions",
                    language = ".NET 8.0",
                    environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Development"
                }
            };

            return new
            {
                jsonrpc = "2.0",
                id = id,
                result = new
                {
                    contents = new[]
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

        return CreateErrorResponse(id, -32602, $"Resource not found: {uri}");
    }

    private async Task<string> CallTool(string? toolName, Dictionary<string, object?> arguments)
    {
        return toolName switch
        {
            "get_server_info" => await _serverInfoTools.GetServerInfo(
                arguments.GetValueOrDefault("includeEnvironment") as bool? ?? false),
            "get_server_status" => await _serverInfoTools.GetServerStatus(),
            "echo" => await _serverInfoTools.Echo(
                arguments.GetValueOrDefault("message")?.ToString() ?? ""),
            _ => throw new ArgumentException($"Unknown tool: {toolName}")
        };
    }

    private static List<object> GetAvailableTools()
    {
        var tools = new List<object>();

        // Define our available tools with proper schemas
        tools.Add(new
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
        });

        tools.Add(new
        {
            name = "get_server_status",
            description = "Get the current status of the server",
            inputSchema = new
            {
                type = "object",
                properties = new { }
            }
        });

        tools.Add(new
        {
            name = "echo",
            description = "Echo a message back to the client",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    message = new
                    {
                        type = "string",
                        description = "The message to echo back"
                    }
                },
                required = new[] { "message" }
            }
        });

        return tools;
    }

    private static object GetJsonElementValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray().Select(GetJsonElementValue).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => GetJsonElementValue(p.Value)),
            _ => element.ToString()
        };
    }

    private static object CreateErrorResponse(object? id, int code, string message)
    {
        return new
        {
            jsonrpc = "2.0",
            id = id,
            error = new { code, message }
        };
    }
}

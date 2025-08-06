# TechCrunch News MCP Server - Streamable HTTP Implementation

This is a Model Context Protocol (MCP) server that provides access to TechCrunch news content using the official MCP Python SDK with **streamable HTTP transport**.

## Features

- **News Fetching**: Get latest news from TechCrunch in various categories
- **Category Support**: ai, startup, security, venture, or latest news
- **Streamable HTTP Transport**: Remote access via HTTP endpoints
- **FastMCP Framework**: Uses the official MCP Python SDK
- **Error Handling**: Robust error handling and logging

## Available Tools

### `fetch_from_techcrunch(category: str = "latest")`
Fetches the latest news from TechCrunch for a specified category.

**Parameters:**
- `category` (optional): News category (ai, startup, security, venture, latest)

**Returns:**
- String containing formatted news content from TechCrunch

## Supported Categories

- `ai` - Artificial Intelligence news
- `startup` - Startup news and funding
- `security` - Cybersecurity news  
- `venture` - Venture capital news
- `latest` - Latest general news (default)
4. **Proper application state management** - no mock database, clean state handling
5. **Explicit port configuration** - runs on port 8000 with `/mcp` endpoint

## Transport Compatibility

- ✅ **Streamable HTTP**: Fully supported and recommended for production
- ❌ **SSE (Server-Sent Events)**: Not implemented (VS Code expects this but we use .NET server for VS Code)
- ❌ **Stdio**: Not implemented (not needed for remote deployment)

## Installation

```bash
# Install dependencies
pip install -r requirements.txt
```

## Usage

### Local Development
```bash
python3 app.py
```

Server runs on `http://localhost:8000/mcp`

### Docker
```bash
docker build -t mcp-python-server .
docker run -p 8000:8000 mcp-python-server
```

### Azure Container Apps (via azd)
```bash
azd up
```


## Available Tools

### get_sample_data

Get sample data with optional filtering.

Parameters:

- `count` (int, optional): Number of items to return (default: 5)
- `category` (str, optional): Filter by category (default: "all")

### get_server_info

Get comprehensive server information.

Parameters:

- `include_environment` (bool, optional): Include environment details (default: false)

### get_server_status

Get current operational status including uptime and memory usage.

### echo

Echo messages with optional formatting.

Parameters:

- `message` (str): The message to echo
- `uppercase` (bool, optional): Return message in uppercase (default: false)

## Available Resources

### server://config

Server configuration as JSON:

- Server metadata
- Capabilities
- Runtime information
- Transport configuration

### sample://data

Sample data resource containing:

- Example message
- Timestamp
- Sample data array

## Testing

Test the server with the provided client:

```bash
python3 test_python_mcp.py
```

## Protocol Compliance

- **MCP Protocol Version**: 2024-11-05
- **Transport**: Streamable HTTP only
- **Features**: Tools, Resources, Structured Output, Logging
- **Authentication**: None (basic implementation)

## Dependencies

- `mcp>=1.1.0` - Official MCP Python SDK
- `pydantic>=2.8.0` - Data validation and serialization
- `psutil>=5.9.0` - System information for status monitoring

## MCP Client Usage

Connect to the server using the MCP Python SDK:

```python
import asyncio
from mcp.client.session import ClientSession
from mcp.client.stdio import StdioServerParameters, stdio_client

async def main():
    server_params = StdioServerParameters(
        command="python",
        args=["app.py", "--mcp-only"]
    )
    
    async with stdio_client(server_params) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            
            # List available tools
            tools = await session.list_tools()
            print(f"Available tools: {[t.name for t in tools.tools]}")
            
            # Call a tool
            result = await session.call_tool("get_sample_data", {"count": 3})
            print(f"Result: {result.structuredContent}")

asyncio.run(main())
```

## Development

The server demonstrates modern MCP patterns including:

- **Type Safety**: Uses Pydantic models for structured data
- **Context Management**: Leverages MCP Context for logging and state access
- **Lifecycle Management**: Proper startup/shutdown with dependency injection
- **Error Handling**: Comprehensive error handling with proper MCP error codes
- **Structured Output**: Returns typed data that can be used directly by LLMs

## Docker Support

The server can be containerized using the existing Dockerfile in the project root.

## Azure Deployment

This server is designed to work with Azure Container Apps and Azure API Management as part of the broader MCP infrastructure project.

## Environment Variables

- `MCP_SERVER_PORT`: Port to run the server on (default: 8000)
- `AZURE_CLIENT_ID`: Azure client ID for authentication
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: Application Insights connection string

import os
import uvicorn
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import List, Dict, Any, Optional
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(
    title="MCP Server (Python)",
    description="A Model Context Protocol server implementation in Python",
    version="1.0.0",
)

# Configure CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# MCP Protocol Models
class MCPRequest(BaseModel):
    jsonrpc: str = "2.0"
    id: Optional[str] = None
    method: str
    params: Optional[Dict[str, Any]] = None

class MCPResponse(BaseModel):
    jsonrpc: str = "2.0"
    id: Optional[str] = None
    result: Optional[Dict[str, Any]] = None
    error: Optional[Dict[str, Any]] = None

class Tool(BaseModel):
    name: str
    description: str
    inputSchema: Dict[str, Any]

class Resource(BaseModel):
    uri: str
    name: str
    description: str
    mimeType: str

# Available tools
TOOLS = [
    Tool(
        name="get_sample_data",
        description="Get sample data from the MCP server",
        inputSchema={
            "type": "object",
            "properties": {
                "count": {
                    "type": "integer",
                    "description": "Number of sample items to return",
                    "default": 5
                }
            }
        }
    )
]

# Available resources
RESOURCES = [
    Resource(
        uri="sample://data",
        name="Sample Data",
        description="Sample data resource",
        mimeType="application/json"
    )
]

@app.get("/")
async def root():
    """Health check endpoint"""
    return {
        "status": "healthy",
        "service": "MCP Server (Python)",
        "version": "1.0.0"
    }

@app.post("/mcp")
async def handle_mcp_request(request: MCPRequest) -> MCPResponse:
    """Handle MCP protocol requests"""
    try:
        logger.info(f"Received MCP request: {request.method}")
        
        if request.method == "initialize":
            return MCPResponse(
                id=request.id,
                result={
                    "protocolVersion": "2024-11-05",
                    "capabilities": {
                        "tools": {"listChanged": True},
                        "resources": {"subscribe": True, "listChanged": True}
                    },
                    "serverInfo": {
                        "name": "mcp-server-python",
                        "version": "1.0.0"
                    }
                }
            )
        
        elif request.method == "tools/list":
            return MCPResponse(
                id=request.id,
                result={
                    "tools": [tool.model_dump() for tool in TOOLS]
                }
            )
        
        elif request.method == "tools/call":
            tool_name = request.params.get("name") if request.params else None
            arguments = request.params.get("arguments", {}) if request.params else {}
            
            if tool_name == "get_sample_data":
                count = arguments.get("count", 5)
                sample_data = [
                    {"id": i, "name": f"Item {i}", "value": i * 10}
                    for i in range(1, count + 1)
                ]
                return MCPResponse(
                    id=request.id,
                    result={
                        "content": [
                            {
                                "type": "text",
                                "text": f"Sample data with {count} items: {sample_data}"
                            }
                        ]
                    }
                )
            else:
                return MCPResponse(
                    id=request.id,
                    error={
                        "code": -32601,
                        "message": f"Unknown tool: {tool_name}"
                    }
                )
        
        elif request.method == "resources/list":
            return MCPResponse(
                id=request.id,
                result={
                    "resources": [resource.model_dump() for resource in RESOURCES]
                }
            )
        
        elif request.method == "resources/read":
            uri = request.params.get("uri") if request.params else None
            if uri == "sample://data":
                return MCPResponse(
                    id=request.id,
                    result={
                        "contents": [
                            {
                                "uri": uri,
                                "mimeType": "application/json",
                                "text": '{"message": "This is sample data from the Python MCP server"}'
                            }
                        ]
                    }
                )
            else:
                return MCPResponse(
                    id=request.id,
                    error={
                        "code": -32602,
                        "message": f"Resource not found: {uri}"
                    }
                )
        
        else:
            return MCPResponse(
                id=request.id,
                error={
                    "code": -32601,
                    "message": f"Method not found: {request.method}"
                }
            )
    
    except Exception as e:
        logger.error(f"Error handling MCP request: {e}")
        return MCPResponse(
            id=request.id,
            error={
                "code": -32603,
                "message": f"Internal error: {str(e)}"
            }
        )

if __name__ == "__main__":
    port = int(os.getenv("MCP_SERVER_PORT", "8000"))
    uvicorn.run(app, host="0.0.0.0", port=port)

# Python MCP Server

A Model Context Protocol (MCP) server implementation in Python.

## Features

- FastAPI-based HTTP server
- MCP protocol compliance
- Azure integration
- Containerized deployment

## Development

```bash
# Install dependencies
pip install -r requirements.txt

# Run locally
python app.py
```

## Environment Variables

- `MCP_SERVER_PORT`: Port to run the server on (default: 8000)
- `AZURE_CLIENT_ID`: Azure client ID for authentication
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: Application Insights connection string

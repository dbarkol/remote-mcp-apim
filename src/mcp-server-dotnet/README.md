# .NET MCP Server

A Model Context Protocol (MCP) server implementation in .NET using Azure Functions.

## Features

- Azure Functions runtime
- MCP protocol compliance
- Azure integration
- Serverless deployment

## Development

```bash
# Restore dependencies
dotnet restore

# Run locally
func start
```

## Environment Variables

- `AzureWebJobsStorage`: Storage account connection string
- `AZURE_CLIENT_ID`: Azure client ID for authentication
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: Application Insights connection string

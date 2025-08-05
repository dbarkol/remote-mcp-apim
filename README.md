# Remote MCP with Azure API Management

A complete solution for hosting Model Context Protocol (MCP) servers on Azure with API Management integration. This repository contains multiple MCP server implementations (Python and .NET) designed for secure, scalable deployment to Azure.

## 🏗️ Architecture

This solution deploys:

- **Python MCP Server** - FastAPI-based server running on Azure Container Apps
- **.NET MCP Server** - Azure Functions-based server with isolated .NET runtime
- **Azure API Management** - Centralized API gateway for managing MCP endpoints
- **Azure Container Registry** - Private registry for container images
- **Application Insights** - Monitoring and telemetry for all services
- **Managed Identity** - Secure authentication without secrets

## 🚀 Quick Start

### Prerequisites

- [Azure Developer CLI (azd)](https://aka.ms/azd) v1.17 or above
- Azure subscription
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (optional)
- [Docker](https://www.docker.com/get-started) (for local development)

### 1. Deploy to Azure

Clone this repository and deploy with a single command:

```bash
git clone <your-repo-url>
cd remote-mcp-apim

# Initialize azd environment
azd auth login
azd init

# Deploy everything to Azure
azd up
```

This will provision all Azure resources and deploy both MCP servers.

### 2. Configuration

After deployment, you'll need to configure the following environment variables:

- `AZURE_CLIENT_ID` - Azure client ID for authentication (you'll need to create an app registration)
- Additional MCP-specific configuration as needed

### 3. Test the Services

After deployment, you can test the services:

```bash
# Test Python MCP Server health
curl https://<python-server-url>/

# Test .NET MCP Server health  
curl https://<dotnet-server-url>/

# Test MCP protocol endpoints
curl -X POST https://<server-url>/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":"1","method":"initialize","params":{}}'
```

## 📁 Project Structure

```
remote-mcp-apim/
├── .github/               # GitHub Actions workflows
├── .vscode/               # VS Code configuration
├── infra/                 # Infrastructure as Code (Bicep)
│   ├── main.bicep         # Main infrastructure template
│   ├── main-resources.bicep # Resource definitions
│   └── main.parameters.json # Environment parameters
├── src/
│   ├── mcp-server-python/ # Python MCP server
│   │   ├── app.py         # FastAPI application
│   │   ├── requirements.txt
│   │   ├── Dockerfile
│   │   └── README.md
│   └── mcp-server-dotnet/ # .NET MCP server
│       ├── McpFunctions.cs # Azure Functions
│       ├── Program.cs     # Host configuration
│       ├── *.csproj       # Project file
│       ├── host.json      # Functions host config
│       └── README.md
├── azure.yaml             # Azure Developer CLI configuration
├── .gitignore
├── README.md
└── LICENSE
```

## 🔧 Local Development

### Python MCP Server

```bash
cd src/mcp-server-python

# Create virtual environment
python -m venv .venv
source .venv/bin/activate  # On Windows: .venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt

# Run locally
python app.py
```

### .NET MCP Server

```bash
cd src/mcp-server-dotnet

# Restore dependencies
dotnet restore

# Install Azure Functions Core Tools
npm install -g azure-functions-core-tools@4 --unsafe-perm true

# Run locally
func start
```

## 🔍 MCP Protocol Implementation

Both servers implement the MCP (Model Context Protocol) specification:

### Available Endpoints

- `POST /mcp` - Main MCP protocol endpoint
- `GET /` - Health check endpoint

### Supported MCP Methods

- `initialize` - Initialize the MCP session
- `tools/list` - List available tools
- `tools/call` - Execute a tool
- `resources/list` - List available resources
- `resources/read` - Read resource content

### Example Tools

**Python Server:**
- `get_sample_data` - Returns sample data with configurable count

**.NET Server:**
- `get_server_info` - Returns server information and environment details

## 🛡️ Security

The solution implements security best practices:

- **Managed Identity** - No hardcoded credentials or secrets
- **Private Container Registry** - Images stored in Azure Container Registry
- **CORS Configuration** - Proper cross-origin resource sharing setup
- **Application Insights** - Comprehensive monitoring and logging

## 🌐 API Management Integration

The deployed API Management service provides:

- Centralized API gateway
- Rate limiting and throttling
- API versioning
- Developer portal
- Analytics and monitoring

## 📊 Monitoring

Monitor your MCP servers using:

- **Application Insights** - Application performance monitoring
- **Log Analytics** - Centralized logging
- **Azure Monitor** - Infrastructure monitoring
- **Function App Logs** - Detailed function execution logs

## 🔄 CI/CD

The repository includes GitHub Actions workflows for:

- Automated testing
- Container image building
- Azure deployment
- Infrastructure updates

## 🛠️ Customization

### Adding New Tools

To add new MCP tools:

1. Define the tool schema in the respective server
2. Implement the tool logic
3. Update the tools list endpoint
4. Test locally before deployment

### Scaling

The solution is designed to scale:

- **Container Apps** - Automatic scaling based on demand
- **Azure Functions** - Serverless scaling
- **API Management** - High availability and global distribution

## 📚 Additional Resources

- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- [Azure Developer CLI Documentation](https://docs.microsoft.com/azure/developer/azure-developer-cli/)
- [Azure Container Apps Documentation](https://docs.microsoft.com/azure/container-apps/)
- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Azure API Management Documentation](https://docs.microsoft.com/azure/api-management/)

## 🤝 Contributing

Please read our [Contributing Guide](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

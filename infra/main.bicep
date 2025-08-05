targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Name of the resource group')
param resourceGroupName string = 'rg-${environmentName}'

@description('MCP Server Port')
param mcpServerPort string

@description('Azure Client ID')
param azureClientId string

// Resource token for unique naming
var resourceToken = uniqueString(subscription().id, location, environmentName)
var resourcePrefix = 'mcp'

// Tags to apply to all resources
var tags = {
  'azd-env-name': environmentName
}

// Create resource group
resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// Main deployment module
module main 'main-resources.bicep' = {
  scope: rg
  name: 'main-resources'
  params: {
    location: location
    environmentName: environmentName
    resourceToken: resourceToken
    resourcePrefix: resourcePrefix
    tags: tags
    mcpServerPort: mcpServerPort
    azureClientId: azureClientId
  }
}

// Outputs
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_SUBSCRIPTION_ID string = subscription().subscriptionId
output RESOURCE_GROUP_ID string = rg.id
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = main.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output AZURE_CONTAINER_REGISTRY_NAME string = main.outputs.AZURE_CONTAINER_REGISTRY_NAME
output APPLICATIONINSIGHTS_CONNECTION_STRING string = main.outputs.APPLICATIONINSIGHTS_CONNECTION_STRING
output MCP_PYTHON_SERVER_URL string = main.outputs.MCP_PYTHON_SERVER_URL
output MCP_DOTNET_SERVER_URL string = main.outputs.MCP_DOTNET_SERVER_URL
output AZURE_API_MANAGEMENT_SERVICE_URL string = main.outputs.AZURE_API_MANAGEMENT_SERVICE_URL

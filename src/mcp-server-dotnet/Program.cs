using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Azure.Functions.Worker.ApplicationInsights;
using ModelContextProtocol.Server;
using McpServerDotnet.Tools;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        // Add MCP Server services
        services.AddMcpServer()
            .WithToolsFromAssembly();
            
        // Register our tool classes
        services.AddSingleton<ServerInfoTools>();
    })
    .Build();

host.Run();

using DungeonEscape.Mcp.Services;
using DungeonEscape.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// No command-line or environment configuration can change repository discovery.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = [],
    DisableDefaults = true
});
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddSingleton(new ProjectInfoService(AppContext.BaseDirectory));
builder.Services.AddMcpServer().WithStdioServerTransport().WithTools<ProjectInfoTools>();
await builder.Build().RunAsync();

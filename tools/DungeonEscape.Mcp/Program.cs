using DungeonEscape.Mcp.Services;
using DungeonEscape.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

ProjectInfoService projectInfo;
try
{
    var configuredRoot = RepositoryStartupOptions.ParseRepositoryRoot(args);
    projectInfo = new ProjectInfoService(AppContext.BaseDirectory, configuredRoot);
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine("MCP startup configuration error: " + exception.Message);
    return 2;
}

// Only the explicitly parsed startup option affects discovery; disable generic host configuration.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = [],
    DisableDefaults = true
});
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddSingleton(projectInfo);
builder.Services.AddSingleton<SearchCodeService>();
builder.Services.AddSingleton<ReadScriptService>();
builder.Services.AddSingleton<ListScenesService>();
builder.Services.AddSingleton<UnityConsoleService>();
builder.Services.AddSingleton<GitDiffService>();
builder.Services.AddSingleton<IApprovedTestSuiteExecutor, ApprovedTestSuiteExecutor>();
builder.Services.AddSingleton<RunTestsService>();
builder.Services.AddSingleton<WriteApprovalAuthority>();
builder.Services.AddSingleton<IWriteApprovalAuthority>(services =>
    services.GetRequiredService<WriteApprovalAuthority>());
builder.Services.AddSingleton<ControlledWriteService>();
builder.Services.AddMcpServer().WithStdioServerTransport()
    .WithTools<ProjectInfoTools>()
    .WithTools<SearchCodeTools>()
    .WithTools<ReadScriptTools>()
    .WithTools<ListScenesTools>()
    .WithTools<UnityConsoleTools>()
    .WithTools<GitDiffTools>()
    .WithTools<RunTestsTools>()
    .WithTools<ControlledWriteTools>();
await builder.Build().RunAsync();
return 0;

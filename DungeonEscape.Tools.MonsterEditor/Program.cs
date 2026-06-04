using DungeonEscape.Tools.MonsterEditor.Services;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;

namespace DungeonEscape.Tools.MonsterEditor;

public static class Program
{
    private const string AppIconRelativePath = "wwwroot/icons/DungeonEscape.ico";

    [STAThread]
    public static void Main(string[] args)
    {
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        builder.Services.AddLogging();

        // Application services (singletons - this is a single-window desktop tool).
        builder.Services.AddSingleton<WindowService>();
        builder.Services.AddSingleton<EditorSettingsService>();
        builder.Services.AddSingleton<AssetContext>();

        builder.Services.AddSingleton<MonsterImageCatalog>();
        builder.Services.AddSingleton<ItemImageCatalog>();
        builder.Services.AddSingleton<SpellImageCatalog>();
        builder.Services.AddSingleton<DataFolderService>();
        builder.Services.AddSingleton<DataSourceCatalog>();


        builder.RootComponents.Add<App>("#app");

        var app = builder.Build();

        // Give the dialog/window service access to the native window.
        var windowService = app.Services.GetRequiredService<WindowService>();
        windowService.Attach(app.MainWindow);

        var appIconPath = Path.Combine(AppContext.BaseDirectory, AppIconRelativePath);

        app.MainWindow
            .SetTitle("Dungeon Escape - Data Editor")
            .SetIconFile(appIconPath)
            .SetUseOsDefaultSize(false)
            .SetSize(1280, 800)
            .SetResizable(true)
            .Center();

        AppDomain.CurrentDomain.UnhandledException += (_, error) =>
        {
            var ex = error.ExceptionObject as Exception;
            Console.Error.WriteLine(ex?.ToString() ?? "Unhandled error: unknown exception");
        };

        app.Run();
    }
}

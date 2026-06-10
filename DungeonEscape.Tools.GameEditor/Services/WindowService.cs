using Photino.NET;
using System.IO;

namespace DungeonEscape.Tools.GameEditor.Services;

/// <summary>
/// Thin wrapper around the native Photino window so Blazor components can show
/// OS file dialogs and message boxes without a direct dependency on the host.
/// </summary>
public sealed class WindowService
{
    private PhotinoWindow? window;

    public void Attach(PhotinoWindow nativeWindow) => window = nativeWindow;

    public void SetTitle(string title) => window?.SetTitle(title);

    /// <summary>Show a native open-folder dialog; returns the selected folder or null.</summary>
    public string? ShowOpenFolder(string title, string? initialDirectory)
    {
        if (window == null)
        {
            return null;
        }

        var results = window.ShowOpenFolder(title, initialDirectory, multiSelect: false);
        return results is { Length: > 0 } ? results[0] : null;
    }

    /// <summary>Show a native open-file dialog; returns the selected path or null.</summary>
    public string? ShowOpenFile(string title, string? initialDirectory)

    {
        if (window == null)
        {
            return null;
        }

        var results = window.ShowOpenFile(
            title,
            initialDirectory,
            multiSelect: false,
            filters: new[] { ("JSON files", new[] { "json" }), ("All files", new[] { "*" }) });

        return results is { Length: > 0 } ? results[0] : null;
    }

    /// <summary>Show a native save-file dialog; returns the chosen path or null.</summary>
    public string? ShowSaveFile(string title, string? initialDirectory, string? defaultFileName)
    {
        if (window == null)
        {
            return null;
        }

        var startDirectory = initialDirectory;
        if (!string.IsNullOrEmpty(defaultFileName) && !string.IsNullOrEmpty(startDirectory))
        {
            startDirectory = Path.Combine(startDirectory, defaultFileName);
        }

        return window.ShowSaveFile(
            title,
            startDirectory,
            filters: new[] { ("JSON files", new[] { "json" }) });
    }

    public PhotinoDialogResult ShowConfirm(string title, string message)
    {
        return window?.ShowMessage(
            title,
            message,
            PhotinoDialogButtons.YesNoCancel,
            PhotinoDialogIcon.Warning) ?? PhotinoDialogResult.Cancel;
    }

    public void ShowInfo(string title, string message)
    {
        window?.ShowMessage(title, message);
    }
}

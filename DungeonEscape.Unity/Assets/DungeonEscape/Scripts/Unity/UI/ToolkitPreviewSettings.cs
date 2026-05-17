namespace Redpoint.DungeonEscape.Unity.UI
{
    public static class ToolkitPreviewSettings
    {
        public static void Apply(Settings settings)
        {
            var showPreviews = settings != null && settings.ShowToolkitPreviews;
            TitleMenu.UseToolkitPreview = showPreviews;
            GameMenu.UseToolkitPreview = showPreviews;
            TitleMenu.UseToolkitLoadRenderer = settings != null && settings.UseToolkitTitleLoadRenderer;
        }
    }
}

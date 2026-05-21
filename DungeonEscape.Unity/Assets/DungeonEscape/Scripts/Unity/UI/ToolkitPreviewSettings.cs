namespace Redpoint.DungeonEscape.Unity.UI
{
    public static class ToolkitPreviewSettings
    {
        public static void Apply(Settings settings)
        {
            TitleMenu.UseToolkitPreview = false;
            GameMenu.UseToolkitPreview = false;
            TitleMenu.UseToolkitMainMenuRenderer = true;
            TitleMenu.UseToolkitLoadRenderer = true;
            TitleMenu.UseToolkitCreateRenderer = true;
            GameMenu.UseToolkitModalRenderer = true;
        }
    }
}

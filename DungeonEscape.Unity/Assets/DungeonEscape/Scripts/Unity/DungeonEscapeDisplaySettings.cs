using Redpoint.DungeonEscape;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public static class DungeonEscapeDisplaySettings
    {
        public static void Apply(Settings settings)
        {
            if (settings == null)
            {
                return;
            }

            if (Screen.fullScreen == settings.IsFullScreen)
            {
                return;
            }

            Screen.fullScreenMode = settings.IsFullScreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
            Screen.fullScreen = settings.IsFullScreen;
        }
    }
}

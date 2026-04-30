// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace Redpoint.DungeonEscape
{
    public class InputBinding
    {
        public string Command { get; set; }
        public string Primary { get; set; }
        public string Secondary { get; set; }
        public string Gamepad { get; set; }
    }

    public class Settings
    {
        public bool NoMonsters { get; set; }
        public bool MapDebugInfo { get; set; }

        public float MusicVolume { get; set; } = 0.5f;

        public float SoundEffectsVolume { get; set; } = 0.5f;

        public bool IsFullScreen { get; set; }
        public string Version { get; set; }
        public int MaxPartyMembers { get; set; } = 4;
        public float UiScale { get; set; } = 1f;
        public float SprintBoost { get; set; } = 1.5f;
        public float TurnMoveDelaySeconds { get; set; } = 0.12f;
        public bool AutoSaveEnabled { get; set; } = true;
        public float AutoSaveIntervalSeconds { get; set; } = 5f;
        public bool SkipSplashAndLoadQuickSave { get; set; }
        public bool ShowHiddenObjects { get; set; }
        public string UiBackgroundColor { get; set; } = "#000000";
        public float UiBackgroundAlpha { get; set; } = 1f;
        public string UiHoverColor { get; set; } = "#808080";
        public string UiActiveColor { get; set; } = "#D3D3D3";
        public string UiBorderColor { get; set; } = "#FFFFFF";
        public int UiBorderThickness { get; set; } = 3;
        public string UiTextColor { get; set; } = "#FFFFFF";
        public string UiHighlightColor { get; set; } = "#FFFF00";
        public InputBinding[] InputBindings { get; set; }
    }
}

// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace Redpoint.DungeonEscape
{
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
        public bool ShowHiddenObjects { get; set; }
    }
}

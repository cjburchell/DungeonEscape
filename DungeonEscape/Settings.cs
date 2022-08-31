// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
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
    }
}
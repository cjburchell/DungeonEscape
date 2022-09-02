using System.Collections.Generic;

namespace Redpoint.DungeonEscape
{
    public interface ISounds
    {
        float MusicVolume { get; set; }
        float SoundEffectsVolume { get; set; }
        
        void PlayMusic(IEnumerable<string> songList);
        void PlaySoundEffect(string effectPath, bool wait = false);
        void StopMusic();
    }
}
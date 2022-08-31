namespace Redpoint.DungeonEscape
{
    public interface ISounds
    {
        float MusicVolume { get; set; }
        float SoundEffectsVolume { get; set; }
        
        void PlayMusic(string songPath);
        void PlaySoundEffect(string effectPath, bool wait = false);
        void StopMusic();
    }
}
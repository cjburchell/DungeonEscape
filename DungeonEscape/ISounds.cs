namespace Redpoint.DungeonEscape
{
    public interface ISounds
    {
        void PlayMusic(string songPath);
        void PlaySoundEffect(string effectPath, bool wait = false);
        void StopMusic();
    }
}
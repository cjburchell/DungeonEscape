namespace Redpoint.DungeonEscape
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Xna.Framework.Audio;
    using Microsoft.Xna.Framework.Media;

    public class Sounds : ISounds
    {
        private readonly Dictionary<string, Song> _songs = new Dictionary<string, Song>();
        private readonly Dictionary<string, SoundEffect> _sounds = new Dictionary<string, SoundEffect>();
        private string _currentSong;
        public void PlayMusic(string songPath)
        {
            if (this._currentSong == songPath)
            {
                return;
            }
            
            Song song;
            if (this._songs.ContainsKey(songPath))
            {
                song = this._songs[songPath];
            }
            else
            {

                try
                {
                    song = Song.FromUri(songPath,new Uri($"Content\\sound\\music\\{songPath}.ogg", UriKind.Relative) );
                }
                catch (NoAudioHardwareException)
                {
                    return;
                }
               
                this._songs.Add(songPath, song);
            }
            
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(song);
            this._currentSong = songPath;
        }

        public void PlaySoundEffect(string effectPath, bool wait = false)
        {
            SoundEffect sfx;
            if (this._sounds.ContainsKey(effectPath))
            {
                sfx = this._sounds[effectPath];
            }
            else
            {

                try
                {
                    sfx = SoundEffect.FromFile($"Content\\sound\\fx\\{effectPath}.wav");
                }
                catch (NoAudioHardwareException)
                {
                   return;
                }
                
                this._sounds.Add(effectPath, sfx);
            }
            sfx.Play();
            if (wait)
            {
                Thread.Sleep(sfx.Duration);
            }
        }

        public void StopMusic()
        {
            MediaPlayer.Stop();
        }
    }
}
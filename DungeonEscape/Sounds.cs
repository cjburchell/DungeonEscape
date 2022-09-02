
namespace Redpoint.DungeonEscape
{
    using System.Linq;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Xna.Framework.Audio;
    using Microsoft.Xna.Framework.Media;

    public class Sounds : ISounds
    {
        private readonly Dictionary<string, Song> _songs = new();
        private readonly Dictionary<string, SoundEffect> _sounds = new();
        private string _currentSong;
        private  Dictionary<string, Song> _currentSongList = new ();
        private float _musicVolume = 0.5f;

        public Sounds()
        {
            MediaPlayer.MediaStateChanged += OnMediaStateChanged;
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                MediaPlayer.Volume = value;
                _musicVolume = value;
            }
        }

        public float SoundEffectsVolume { get; set; } = 0.5f;

        public void PlayMusic(IEnumerable<string> songList)
        {
            this._currentSongList.Clear();
            foreach (var songPath in songList)
            {
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
                
                this._currentSongList.Add( songPath, song);
            }

            if (!this._currentSongList.Any())
            {
                this.StopMusic();
                return;
            }
            
            
            if (this._currentSong != null && this._currentSongList.ContainsKey(this._currentSong))
            {
                return;
            }

            PlayNextSong();
        }

        private void PlaySong(KeyValuePair<string, Song> song)
        {
            this._currentSong = song.Key;
            MediaPlayer.Volume = this.MusicVolume;
            MediaPlayer.Play(song.Value);
        }

        private KeyValuePair<string, Song> CalculateNextSong()
        {
            var nextSong = this._currentSongList.Count == 1
                ? this._currentSongList.First()
                : this._currentSongList.Where(i => i.Key != this._currentSong).ToArray()[
                    Nez.Random.NextInt(this._currentSongList.Count(i => i.Key != this._currentSong))];
            return nextSong;
        }

        private void OnMediaStateChanged(object sender, EventArgs args)
        {
            if (MediaPlayer.State != MediaState.Stopped || !this._currentSongList.Any())
            {
                return;
            }

            PlayNextSong();
        }

        private void PlayNextSong()
        {
            PlaySong(CalculateNextSong());
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
            sfx.Play(this.SoundEffectsVolume, 0,0);
            if (wait)
            {
                Thread.Sleep(sfx.Duration);
            }
        }

        public void StopMusic()
        {
            this._currentSongList.Clear();
            this._currentSong = null;
            MediaPlayer.Stop();
        }
    }
}
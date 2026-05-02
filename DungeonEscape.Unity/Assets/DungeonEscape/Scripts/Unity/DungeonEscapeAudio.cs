using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Redpoint.DungeonEscape;
using Redpoint.DungeonEscape.State;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeAudio : MonoBehaviour, ISounds
    {
        private const string MusicFolder = "Assets/DungeonEscape/Audio/music/";
        private const string EffectsFolder = "Assets/DungeonEscape/Audio/fx/";

        private static DungeonEscapeAudio instance;

        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private AudioSource musicSource;
        private AudioSource effectsSource;
        private string currentMusicName;
        private string pendingMusicName;
        private string currentMapMusicName;

        public static DungeonEscapeAudio GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindAnyObjectByType<DungeonEscapeAudio>();
            if (instance != null)
            {
                instance.EnsureSources();
                return instance;
            }

            var gameObject = new GameObject("DungeonEscapeAudio");
            DontDestroyOnLoad(gameObject);
            instance = gameObject.AddComponent<DungeonEscapeAudio>();
            instance.EnsureSources();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSources();
            ApplySettings(DungeonEscapeSettingsCache.Current);
        }

        private void EnsureSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.GetComponent<AudioSource>();
                if (musicSource == null)
                {
                    musicSource = gameObject.AddComponent<AudioSource>();
                }
            }

            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;

            if (effectsSource == null || effectsSource == musicSource)
            {
                var sources = gameObject.GetComponents<AudioSource>();
                effectsSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
            }

            effectsSource.loop = false;
            effectsSource.playOnAwake = false;
            effectsSource.spatialBlend = 0f;

            if (FindAnyObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
        }

        public void ApplySettings(Settings settings)
        {
            if (musicSource != null)
            {
                musicSource.volume = ClampVolume(settings == null ? 0.5f : settings.MusicVolume);
                musicSource.mute = false;
            }

            if (effectsSource != null)
            {
                effectsSource.volume = ClampVolume(settings == null ? 0.5f : settings.SoundEffectsVolume);
                effectsSource.mute = false;
            }
        }

        public void PlayMapMusic(string name)
        {
            currentMapMusicName = NormalizeName(name);
            PlayMusic(currentMapMusicName);
        }

        public void PlayBiomeMusic(Biome biome)
        {
            var biomeMusicName = GetBiomeMusicName(biome);
            PlayMusic(string.IsNullOrEmpty(biomeMusicName) ? currentMapMusicName : biomeMusicName);
        }

        public void PlayMusic(string name)
        {
            var normalizedName = NormalizeName(name);
            if (string.IsNullOrEmpty(normalizedName))
            {
                return;
            }

            if (string.Equals(currentMusicName, normalizedName, StringComparison.OrdinalIgnoreCase) &&
                musicSource != null &&
                musicSource.isPlaying)
            {
                return;
            }

            if (string.Equals(pendingMusicName, normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            pendingMusicName = normalizedName;
            StartCoroutine(LoadAndPlayMusic(normalizedName));
        }

        public void PlaySoundEffect(string name, bool stopCurrent = false)
        {
            var normalizedName = NormalizeName(name);
            if (string.IsNullOrEmpty(normalizedName))
            {
                return;
            }

            StartCoroutine(LoadAndPlayEffect(normalizedName, stopCurrent));
        }

        private IEnumerator LoadAndPlayMusic(string name)
        {
            AudioClip clip = null;
            yield return LoadClip(GetMusicKey(name), MusicFolder + name + ".ogg", loaded => clip = loaded);
            if (!string.Equals(pendingMusicName, name, StringComparison.OrdinalIgnoreCase) || clip == null)
            {
                yield break;
            }

            musicSource.clip = clip;
            currentMusicName = name;
            pendingMusicName = null;
            musicSource.Play();
            Debug.Log("Playing music: " + name + " volume " + musicSource.volume.ToString("0.00"));
        }

        private IEnumerator LoadAndPlayEffect(string name, bool stopCurrent)
        {
            AudioClip clip = null;
            yield return LoadClip(GetEffectKey(name), EffectsFolder + name + ".wav", loaded => clip = loaded);
            if (clip == null)
            {
                yield break;
            }

            if (stopCurrent)
            {
                effectsSource.Stop();
            }

            effectsSource.PlayOneShot(clip);
            Debug.Log("Playing sound effect: " + name + " volume " + effectsSource.volume.ToString("0.00"));
        }

        private IEnumerator LoadClip(string key, string assetPath, Action<AudioClip> onLoaded)
        {
            AudioClip clip;
            if (clips.TryGetValue(key, out clip))
            {
                onLoaded(clip);
                yield break;
            }

#if UNITY_EDITOR
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip != null)
            {
                clips[key] = clip;
                onLoaded(clip);
                yield break;
            }
#endif

            var fullPath = UnityAssetPath.ToRuntimePath(assetPath);
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                Debug.LogWarning("Audio asset not found: " + assetPath + " resolved to " + (fullPath ?? "<null>"));
                onLoaded(null);
                yield break;
            }

            using (var request = UnityWebRequestMultimedia.GetAudioClip(
                       new Uri(fullPath).AbsoluteUri,
                       GetAudioType(fullPath)))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Audio asset failed to load: " + assetPath + " " + request.error);
                    onLoaded(null);
                    yield break;
                }

                clip = DownloadHandlerAudioClip.GetContent(request);
                clips[key] = clip;
                onLoaded(clip);
            }
        }

        private static string GetBiomeMusicName(Biome biome)
        {
            switch (biome)
            {
                case Biome.Cave:
                    return "underground-town";
                case Biome.Town:
                    return "in-the-city";
                case Biome.Tower:
                    return "like-totally-rad";
                case Biome.Desert:
                    return "unprepared";
                case Biome.Water:
                case Biome.Grassland:
                case Biome.Forest:
                case Biome.Hills:
                case Biome.Swamp:
                case Biome.None:
                default:
                    return null;
            }
        }

        private static string NormalizeName(string name)
        {
            return string.IsNullOrEmpty(name) ? null : Path.GetFileNameWithoutExtension(name.Trim());
        }

        private static float ClampVolume(float value)
        {
            return Mathf.Clamp01(value);
        }

        private static string GetMusicKey(string name)
        {
            return "music/" + name;
        }

        private static string GetEffectKey(string name)
        {
            return "fx/" + name;
        }

        private static AudioType GetAudioType(string path)
        {
            var extension = Path.GetExtension(path);
            if (string.Equals(extension, ".ogg", StringComparison.OrdinalIgnoreCase))
            {
                return AudioType.OGGVORBIS;
            }

            if (string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase))
            {
                return AudioType.WAV;
            }

            return AudioType.UNKNOWN;
        }
    }
}

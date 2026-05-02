using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed class SplashScreen : MonoBehaviour
    {
        private const float FadeInSeconds = 1f;
        private const float HoldSeconds = 1.5f;
        private const float FadeOutSeconds = 0.5f;
        private const float DurationSeconds = FadeInSeconds + HoldSeconds + FadeOutSeconds;
        private const int SplashGuiDepth = -4000;
        private const string SplashTextureAssetPath = "Assets/DungeonEscape/Images/ui/splash.png";

        private static bool isVisible;
        private Texture2D splashTexture;
        private float startTime;
        private bool hasDrawn;
        private bool hasStartedTimer;

        public static bool IsVisible
        {
            get { return isVisible; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBeforeSceneLoad()
        {
            if (SettingsCache.Current.SkipSplashAndLoadQuickSave)
            {
                return;
            }

            if (isVisible || FindAnyObjectByType<SplashScreen>() != null)
            {
                return;
            }

            var splashScreen = new GameObject("SplashScreen");
            DontDestroyOnLoad(splashScreen);
            splashScreen.AddComponent<SplashScreen>();
        }

        private void Awake()
        {
            isVisible = true;
            startTime = -1f;
            splashTexture = LoadTexture(SplashTextureAssetPath);
            Audio.GetOrCreate().PlayMusic("first-story");
        }

        private void Update()
        {
            if (!hasDrawn)
            {
                return;
            }

            if (!hasStartedTimer)
            {
                hasStartedTimer = true;
                startTime = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - startTime < DurationSeconds)
            {
                return;
            }

            isVisible = false;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (isVisible)
            {
                isVisible = false;
            }
        }

        private void OnGUI()
        {
            if (!hasDrawn)
            {
                hasDrawn = true;
            }

            var previousDepth = GUI.depth;
            var previousColor = GUI.color;

            GUI.depth = SplashGuiDepth;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            var alpha = GetSplashAlpha();
            if (splashTexture == null)
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.Max(24, Mathf.RoundToInt(Screen.height * 0.08f)),
                    normal = { textColor = Color.white }
                };
                GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), "Dungeon Escape", style);
            }
            else
            {
                var imageWidth = splashTexture.width;
                var imageHeight = splashTexture.height;
                var scale = Mathf.Min(Screen.width / imageWidth, Screen.height / imageHeight);
                var width = imageWidth * scale;
                var height = imageHeight * scale;
                var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
                GUI.DrawTexture(rect, splashTexture, ScaleMode.ScaleToFit, true);
            }

            GUI.color = new Color(0f, 0f, 0f, 1f - alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }

        private float GetSplashAlpha()
        {
            if (!hasStartedTimer || startTime < 0f)
            {
                return 0f;
            }

            var elapsed = Time.unscaledTime - startTime;
            if (elapsed < FadeInSeconds)
            {
                return Mathf.Clamp01(elapsed / FadeInSeconds);
            }

            if (elapsed < FadeInSeconds + HoldSeconds)
            {
                return 1f;
            }

            return Mathf.Clamp01(1f - ((elapsed - FadeInSeconds - HoldSeconds) / FadeOutSeconds));
        }

        private static Texture2D LoadTexture(string assetPath)
        {
#if UNITY_EDITOR
            var editorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (editorTexture != null)
            {
                return editorTexture;
            }
#endif

            var fullPath = UnityAssetPath.ToRuntimePath(assetPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Splash image not found: " + assetPath);
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(fullPath)))
            {
                Debug.LogWarning("Could not load splash image: " + assetPath);
                return null;
            }

            texture.name = "DungeonEscapeSplash";
            return texture;
        }
    }
}

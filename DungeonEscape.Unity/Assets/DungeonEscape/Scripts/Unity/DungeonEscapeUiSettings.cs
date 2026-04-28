using Redpoint.DungeonEscape;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeUiSettings : MonoBehaviour
    {
        [SerializeField]
        private float uiScale = 1f;

        [SerializeField]
        private int referenceHeight = 1080;

        private Settings settings;

        public float UiScale
        {
            get { return Mathf.Max(0.5f, uiScale); }
            set { uiScale = Mathf.Max(0.5f, value); }
        }

        public int ReferenceHeight
        {
            get { return Mathf.Max(1, referenceHeight); }
        }

        public float PixelScale
        {
            get { return UiScale * Screen.height / ReferenceHeight; }
        }

        public Settings LoadedSettings
        {
            get { return settings; }
        }

        public void ApplySettings(Settings value)
        {
            settings = value;
            UiScale = settings == null || settings.UiScale <= 0f ? 1f : settings.UiScale;
        }

        public static DungeonEscapeUiSettings GetOrCreate()
        {
            var settings = FindAnyObjectByType<DungeonEscapeUiSettings>();
            if (settings != null)
            {
                if (settings.LoadedSettings == null)
                {
                    settings.ApplySettings(DungeonEscapeSettingsCache.Current);
                }

                return settings;
            }

            var created = new GameObject("DungeonEscapeUiSettings").AddComponent<DungeonEscapeUiSettings>();
            created.ApplySettings(DungeonEscapeSettingsCache.Current);
            return created;
        }
    }
}

using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeMessageBox : MonoBehaviour
    {
        private string speaker;
        private string message;
        private GUIStyle boxStyle;
        private GUIStyle speakerStyle;
        private GUIStyle messageStyle;
        private Texture2D backgroundTexture;
        private DungeonEscapeUiSettings uiSettings;
        private float lastPixelScale;

        public bool IsVisible
        {
            get { return !string.IsNullOrEmpty(message); }
        }

        public void Show(string speakerName, string text)
        {
            speaker = speakerName;
            message = text;
        }

        public void Hide()
        {
            speaker = null;
            message = null;
        }

        private void OnGUI()
        {
            if (!IsVisible)
            {
                return;
            }

            EnsureStyles();

            var scale = GetPixelScale();
            var margin = 24f * scale;
            var paddingX = 18f * scale;
            var paddingY = 12f * scale;
            var speakerHeight = 24f * scale;
            var speakerGap = 6f * scale;
            var width = Mathf.Min(Screen.width - 32f * scale, 760f * scale);
            var height = 120f * scale;
            var rect = new Rect((Screen.width - width) / 2f, Screen.height - height - margin, width, height);
            GUI.Box(rect, GUIContent.none, boxStyle);

            var contentRect = new Rect(rect.x + paddingX, rect.y + paddingY, rect.width - paddingX * 2f, rect.height - paddingY * 2f);
            if (!string.IsNullOrEmpty(speaker))
            {
                GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, speakerHeight), speaker, speakerStyle);
                GUI.Label(new Rect(contentRect.x, contentRect.y + speakerHeight + speakerGap, contentRect.width, contentRect.height - speakerHeight - speakerGap), message, messageStyle);
            }
            else
            {
                GUI.Label(contentRect, message, messageStyle);
            }
        }

        private void EnsureStyles()
        {
            var scale = GetPixelScale();
            if (boxStyle != null && Mathf.Approximately(lastPixelScale, scale))
            {
                return;
            }

            lastPixelScale = scale;
            boxStyle = new GUIStyle(GUI.skin.box);
            if (backgroundTexture == null)
            {
                backgroundTexture = new Texture2D(1, 1);
                backgroundTexture.SetPixel(0, 0, new Color(0.05f, 0.06f, 0.07f, 0.94f));
                backgroundTexture.Apply();
            }

            boxStyle.normal.background = backgroundTexture;
            boxStyle.normal.textColor = Color.white;

            speakerStyle = new GUIStyle(GUI.skin.label);
            speakerStyle.fontSize = Mathf.RoundToInt(18f * scale);
            speakerStyle.fontStyle = FontStyle.Bold;
            speakerStyle.normal.textColor = Color.white;

            messageStyle = new GUIStyle(GUI.skin.label);
            messageStyle.fontSize = Mathf.RoundToInt(16f * scale);
            messageStyle.wordWrap = true;
            messageStyle.normal.textColor = Color.white;
        }

        private float GetPixelScale()
        {
            if (uiSettings == null)
            {
                uiSettings = DungeonEscapeUiSettings.GetOrCreate();
            }

            return uiSettings.PixelScale;
        }
    }
}

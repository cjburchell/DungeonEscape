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

            var width = Mathf.Min(Screen.width - 32, 760);
            var height = 120;
            var rect = new Rect((Screen.width - width) / 2f, Screen.height - height - 24, width, height);
            GUI.Box(rect, GUIContent.none, boxStyle);

            var contentRect = new Rect(rect.x + 18, rect.y + 12, rect.width - 36, rect.height - 24);
            if (!string.IsNullOrEmpty(speaker))
            {
                GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, 24), speaker, speakerStyle);
                GUI.Label(new Rect(contentRect.x, contentRect.y + 30, contentRect.width, contentRect.height - 30), message, messageStyle);
            }
            else
            {
                GUI.Label(contentRect, message, messageStyle);
            }
        }

        private void EnsureStyles()
        {
            if (boxStyle != null)
            {
                return;
            }

            boxStyle = new GUIStyle(GUI.skin.box);
            backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0.05f, 0.06f, 0.07f, 0.94f));
            backgroundTexture.Apply();
            boxStyle.normal.background = backgroundTexture;
            boxStyle.normal.textColor = Color.white;

            speakerStyle = new GUIStyle(GUI.skin.label);
            speakerStyle.fontSize = 18;
            speakerStyle.fontStyle = FontStyle.Bold;
            speakerStyle.normal.textColor = Color.white;

            messageStyle = new GUIStyle(GUI.skin.label);
            messageStyle.fontSize = 16;
            messageStyle.wordWrap = true;
            messageStyle.normal.textColor = Color.white;
        }
    }
}

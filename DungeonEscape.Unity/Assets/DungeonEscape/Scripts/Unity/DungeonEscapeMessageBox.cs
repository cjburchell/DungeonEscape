using System;
using System.Collections.Generic;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeMessageBox : MonoBehaviour
    {
        private const float InitialNavigationRepeatDelay = 0.35f;
        private const float NavigationRepeatDelay = 0.12f;

        private string speaker;
        private string message;
        private GUIStyle boxStyle;
        private GUIStyle speakerStyle;
        private GUIStyle messageStyle;
        private GUIStyle choiceStyle;
        private GUIStyle selectedChoiceStyle;
        private Texture2D backgroundTexture;
        private DungeonEscapeUiSettings uiSettings;
        private float lastPixelScale;
        private List<string> choices;
        private Action<int> choiceSelected;
        private int selectedChoiceIndex;
        private int acceptInputAfterFrame;
        private int repeatingChoiceMoveY;
        private float nextChoiceMoveTime;

        public bool IsVisible
        {
            get { return !string.IsNullOrEmpty(message); }
        }

        public bool HasChoices
        {
            get { return choices != null && choices.Count > 0; }
        }

        public void Show(string speakerName, string text)
        {
            speaker = speakerName;
            message = text;
            choices = null;
            choiceSelected = null;
            selectedChoiceIndex = 0;
            acceptInputAfterFrame = Time.frameCount;
            ResetChoiceNavigationRepeat();
        }

        public void Show(string speakerName, string text, IEnumerable<string> choiceLabels, Action<int> selected)
        {
            speaker = speakerName;
            message = text;
            choices = new List<string>();
            if (choiceLabels != null)
            {
                choices.AddRange(choiceLabels);
            }

            choiceSelected = selected;
            selectedChoiceIndex = 0;
            acceptInputAfterFrame = Time.frameCount + 1;
            ResetChoiceNavigationRepeat();
        }

        public void Hide()
        {
            speaker = null;
            message = null;
            choices = null;
            choiceSelected = null;
            selectedChoiceIndex = 0;
            ResetChoiceNavigationRepeat();
        }

        public void ConfirmOrHide()
        {
            if (!HasChoices)
            {
                Hide();
                return;
            }

            SelectChoice(selectedChoiceIndex);
        }

        private void Update()
        {
            if (!IsVisible)
            {
                return;
            }

            if (Time.frameCount <= acceptInputAfterFrame)
            {
                return;
            }

            if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Cancel))
            {
                Hide();
                return;
            }

            if (!HasChoices)
            {
                return;
            }

            var moveY = GetChoiceMoveY();
            if (moveY < 0)
            {
                selectedChoiceIndex = Mathf.Max(0, selectedChoiceIndex - 1);
            }
            else if (moveY > 0)
            {
                selectedChoiceIndex = Mathf.Min(choices.Count - 1, selectedChoiceIndex + 1);
            }
            else if (DungeonEscapeInput.GetCommandDown(DungeonEscapeInputCommand.Interact))
            {
                SelectChoice(selectedChoiceIndex);
            }
        }

        private int GetChoiceMoveY()
        {
            var pressed = DungeonEscapeInput.GetMoveYDown();
            if (pressed != 0)
            {
                repeatingChoiceMoveY = pressed;
                nextChoiceMoveTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return pressed;
            }

            var held = DungeonEscapeInput.GetMoveY();
            if (held == 0)
            {
                ResetChoiceNavigationRepeat();
                return 0;
            }

            if (held != repeatingChoiceMoveY)
            {
                repeatingChoiceMoveY = held;
                nextChoiceMoveTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return held;
            }

            if (Time.unscaledTime < nextChoiceMoveTime)
            {
                return 0;
            }

            nextChoiceMoveTime = Time.unscaledTime + NavigationRepeatDelay;
            return held;
        }

        private void ResetChoiceNavigationRepeat()
        {
            repeatingChoiceMoveY = 0;
            nextChoiceMoveTime = 0f;
        }

        private void OnGUI()
        {
            if (!IsVisible)
            {
                return;
            }

            EnsureStyles();

            var visibleChoices = choices;
            var hasChoices = visibleChoices != null && visibleChoices.Count > 0;
            var scale = GetPixelScale();
            var margin = 24f * scale;
            var paddingX = 18f * scale;
            var paddingY = 12f * scale;
            var speakerHeight = 24f * scale;
            var speakerGap = 6f * scale;
            var width = Mathf.Min(Screen.width - 32f * scale, 760f * scale);
            var choiceHeight = 28f * scale;
            var choiceGap = 4f * scale;
            var choiceAreaHeight = hasChoices ? (visibleChoices.Count * (choiceHeight + choiceGap)) : 0f;
            var height = (120f * scale) + choiceAreaHeight;
            var rect = new Rect((Screen.width - width) / 2f, Screen.height - height - margin, width, height);
            GUI.Box(rect, GUIContent.none, boxStyle);

            var contentRect = new Rect(rect.x + paddingX, rect.y + paddingY, rect.width - paddingX * 2f, rect.height - paddingY * 2f);
            var textHeight = hasChoices ? contentRect.height - choiceAreaHeight - speakerGap : contentRect.height;
            if (!string.IsNullOrEmpty(speaker))
            {
                GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, speakerHeight), speaker, speakerStyle);
                GUI.Label(new Rect(contentRect.x, contentRect.y + speakerHeight + speakerGap, contentRect.width, textHeight - speakerHeight - speakerGap), message, messageStyle);
            }
            else
            {
                GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, textHeight), message, messageStyle);
            }

            if (hasChoices)
            {
                var y = contentRect.y + textHeight;
                for (var i = 0; i < visibleChoices.Count; i++)
                {
                    var choiceRect = new Rect(contentRect.x, y, contentRect.width, choiceHeight);
                    var style = i == selectedChoiceIndex ? selectedChoiceStyle : choiceStyle;
                    if (GUI.Button(choiceRect, visibleChoices[i], style))
                    {
                        SelectChoice(i);
                        break;
                    }

                    y += choiceHeight + choiceGap;
                }
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

            choiceStyle = new GUIStyle(GUI.skin.button);
            choiceStyle.fontSize = Mathf.RoundToInt(16f * scale);
            choiceStyle.alignment = TextAnchor.MiddleLeft;
            choiceStyle.normal.textColor = Color.white;
            choiceStyle.hover.textColor = Color.white;

            selectedChoiceStyle = new GUIStyle(choiceStyle);
            selectedChoiceStyle.normal.textColor = Color.yellow;
        }

        private void SelectChoice(int index)
        {
            if (!HasChoices || index < 0 || index >= choices.Count)
            {
                return;
            }

            var selected = choiceSelected;
            Hide();
            if (selected != null)
            {
                selected(index);
            }
        }

        private float GetPixelScale()
        {
            if (uiSettings == null)
            {
                uiSettings = DungeonEscapeUiSettings.GetOrCreate();
            }

            return uiSettings == null ? 1f : uiSettings.PixelScale;
        }
    }
}

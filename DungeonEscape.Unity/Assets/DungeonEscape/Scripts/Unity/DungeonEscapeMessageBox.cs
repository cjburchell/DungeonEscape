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
        private DungeonEscapeUiSettings uiSettings;
        private float lastPixelScale;
        private string lastThemeSignature;
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
            var borderThickness = GetBorderThickness();
            var margin = 24f * scale;
            var paddingX = 18f * scale;
            var paddingY = 12f * scale;
            var speakerHeight = 24f * scale;
            var speakerGap = 6f * scale;
            var width = Mathf.Min(Screen.width - 32f * scale, 760f * scale);
            var choiceHeight = 34f * scale;
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
                    var innerChoiceRect = new Rect(
                        choiceRect.x + borderThickness,
                        choiceRect.y + borderThickness,
                        choiceRect.width - borderThickness * 2f,
                        choiceRect.height - borderThickness * 2f);
                    if (GUI.Button(choiceRect, GUIContent.none, style))
                    {
                        SelectChoice(i);
                        break;
                    }

                    GUI.Label(innerChoiceRect, visibleChoices[i], style);
                    y += choiceHeight + choiceGap;
                }
            }
        }

        private void EnsureStyles()
        {
            var scale = GetPixelScale();
            var settings = DungeonEscapeSettingsCache.Current;
            var themeSignature = DungeonEscapeUiTheme.GetSignature(settings);
            if (boxStyle != null &&
                Mathf.Approximately(lastPixelScale, scale) &&
                string.Equals(lastThemeSignature, themeSignature, StringComparison.Ordinal))
            {
                return;
            }

            lastPixelScale = scale;
            lastThemeSignature = themeSignature;
            var theme = DungeonEscapeUiTheme.Create(settings, scale);
            boxStyle = theme.PanelStyle;
            speakerStyle = new GUIStyle(theme.LabelStyle)
            {
                fontSize = Mathf.RoundToInt(18f * scale),
                fontStyle = FontStyle.Bold
            };
            messageStyle = new GUIStyle(theme.LabelStyle)
            {
                fontSize = Mathf.RoundToInt(16f * scale),
                wordWrap = true
            };
            choiceStyle = new GUIStyle(theme.RowStyle)
            {
                fontSize = Mathf.RoundToInt(16f * scale),
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Normal
            };
            choiceStyle.normal.textColor = theme.TextColor;
            selectedChoiceStyle = new GUIStyle(theme.SelectedRowStyle)
            {
                fontSize = Mathf.RoundToInt(16f * scale),
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Normal
            };
            selectedChoiceStyle.normal.textColor = theme.HighlightColor;
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

        private static int GetBorderThickness()
        {
            return DungeonEscapeUiTheme.GetBorderThickness(DungeonEscapeSettingsCache.Current);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Unity.Core;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.UI
{
    public sealed partial class CombatWindow
    {
        private string GetEncounterMessage()
        {
            return monsters.Count == 1
                ? "You have encountered a " + monsters[0].Instance.Name + "!"
                : "You have encountered " + monsters.Count + " enemies!";
        }

        private void CreateMonsterInstances(IEnumerable<Monster> encounterMonsters)
        {
            foreach (var monsterGroup in encounterMonsters.OrderBy(monster => monster.MinLevel).GroupBy(monster => monster.Name))
            {
                var monsterId = 'A';
                foreach (var monster in monsterGroup)
                {
                    var instance = new MonsterInstance(monster, gameState);
                    if (monsterGroup.Count() != 1)
                    {
                        instance.Name = instance.Name + " " + monsterId;
                        monsterId++;
                    }

                    monsters.Add(new CombatMonster
                    {
                        Data = monster,
                        Instance = instance
                    });
                }
            }
        }

        private void ShowMessage(string text, Action next)
        {
            state = CombatState.Message;
            selectedMenuIndex = 0;
            messageText = text;
            afterMessage = next;
            messageScrollPosition = Vector2.zero;
            StartTextReveal();
        }

        private void ContinueMessage()
        {
            if (state != CombatState.Message)
            {
                return;
            }

            if (!IsTextFullyRevealed)
            {
                UiControls.PlayConfirmSound();
                FinishTextReveal();
                return;
            }

            var next = afterMessage;
            afterMessage = null;
            if (next == null)
            {
                Close();
                return;
            }

            next();
        }

        private bool IsTextFullyRevealed
        {
            get { return string.IsNullOrEmpty(messageText) || visibleMessageCharacters >= messageText.Length; }
        }

        private string DisplayedMessage
        {
            get
            {
                if (string.IsNullOrEmpty(messageText) || IsTextFullyRevealed)
                {
                    return messageText;
                }

                return messageText.Substring(0, Mathf.Clamp(visibleMessageCharacters, 0, messageText.Length));
            }
        }

        private void StartTextReveal()
        {
            revealCharacterAccumulator = 0f;
            visibleMessageCharacters = GetTextRevealSpeed() <= 0f || string.IsNullOrEmpty(messageText)
                ? string.IsNullOrEmpty(messageText) ? 0 : messageText.Length
                : 0;
        }

        private void AdvanceTextReveal()
        {
            if (IsTextFullyRevealed)
            {
                return;
            }

            var speed = GetTextRevealSpeed();
            if (speed <= 0f)
            {
                FinishTextReveal();
                return;
            }

            revealCharacterAccumulator += speed * Time.unscaledDeltaTime;
            var charactersToAdd = Mathf.FloorToInt(revealCharacterAccumulator);
            if (charactersToAdd <= 0)
            {
                return;
            }

            revealCharacterAccumulator -= charactersToAdd;
            visibleMessageCharacters = Mathf.Min(messageText.Length, visibleMessageCharacters + charactersToAdd);
        }

        private void FinishTextReveal()
        {
            visibleMessageCharacters = string.IsNullOrEmpty(messageText) ? 0 : messageText.Length;
            revealCharacterAccumulator = 0f;
        }

        private static float GetTextRevealSpeed()
        {
            var settings = SettingsCache.Current;
            return settings == null ? 60f : settings.DialogTextCharactersPerSecond;
        }
    }
}

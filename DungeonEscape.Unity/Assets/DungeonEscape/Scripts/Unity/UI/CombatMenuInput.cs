using Redpoint.DungeonEscape.Unity.Core;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity.UI
{
    internal sealed class CombatMenuInput
    {
        private const float InitialNavigationRepeatDelay = 0.35f;
        private const float NavigationRepeatDelay = 0.12f;

        private int repeatingMoveY;
        private int repeatingMoveX;
        private float nextMoveYTime;
        private float nextMoveXTime;
        private int acceptInteractAfterFrame;
        private bool waitForInteractRelease;

        public int GetMoveY()
        {
            var held = InputManager.GetUiMoveYWithRightStick();
            if (held == 0)
            {
                ResetMoveYRepeat();
                return 0;
            }

            if (held != repeatingMoveY)
            {
                repeatingMoveY = held;
                nextMoveYTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return held;
            }

            if (Time.unscaledTime < nextMoveYTime)
            {
                return 0;
            }

            nextMoveYTime = Time.unscaledTime + NavigationRepeatDelay;
            return held;
        }

        public int GetMoveX()
        {
            var held = InputManager.GetUiMoveXWithRightStick();
            if (held == 0)
            {
                ResetMoveXRepeat();
                return 0;
            }

            if (held != repeatingMoveX)
            {
                repeatingMoveX = held;
                nextMoveXTime = Time.unscaledTime + InitialNavigationRepeatDelay;
                return held;
            }

            if (Time.unscaledTime < nextMoveXTime)
            {
                return 0;
            }

            nextMoveXTime = Time.unscaledTime + NavigationRepeatDelay;
            return held;
        }

        public void BlockInteractUntilRelease()
        {
            acceptInteractAfterFrame = Time.frameCount + 1;
            waitForInteractRelease = true;
        }

        public bool CanAcceptInteract()
        {
            if (Time.frameCount <= acceptInteractAfterFrame)
            {
                return false;
            }

            if (!waitForInteractRelease)
            {
                return true;
            }

            if (InputManager.GetCommand(InputCommand.Interact))
            {
                return false;
            }

            waitForInteractRelease = false;
            return true;
        }

        private void ResetMoveYRepeat()
        {
            repeatingMoveY = 0;
            nextMoveYTime = 0f;
        }

        private void ResetMoveXRepeat()
        {
            repeatingMoveX = 0;
            nextMoveXTime = 0f;
        }
    }
}

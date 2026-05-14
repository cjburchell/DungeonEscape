using System;

namespace Redpoint.DungeonEscape.Unity.UI
{
    internal sealed class CombatButton
    {
        public CombatButton(string label, Action action)
        {
            Label = label;
            Action = action;
        }

        public string Label { get; private set; }
        public Action Action { get; private set; }
    }
}

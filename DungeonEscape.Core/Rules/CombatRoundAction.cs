using System.Collections.Generic;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Rules
{
    public sealed class CombatRoundAction
    {
        public IFighter Source { get; set; }
        public CombatRoundActionState State { get; set; }
        public Spell Spell { get; set; }
        public ItemInstance Item { get; set; }
        public Skill Skill { get; set; }
        public MonsterAction MonsterAction { get; set; }
        public List<IFighter> Targets { get; set; }
        public int InitiativeRoll { get; set; }
        public int InitiativeTotal { get; set; }
    }
}

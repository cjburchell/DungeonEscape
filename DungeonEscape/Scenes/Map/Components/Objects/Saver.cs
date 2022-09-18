namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System.Linq;
    using Common.Components.UI;
    using Nez.AI.Pathfinding;
    using Nez.Tiled;
    using State;
    using UI;

    public class Saver : Sprite
    {
        private readonly UiSystem _ui;
        
        public Saver(TmxObject tmxObject, SpriteState state, TmxMap map, IGame gameState, AstarGridGraph graph, UiSystem ui) : base(tmxObject, state, map, gameState, graph)
        {
            this._ui = ui;
        }
        
        public override bool OnAction(Party party)
        {
            this.GameState.IsPaused = true;
            var questionWindow = new QuestionWindow(this._ui);
            questionWindow.Show($"{this.SpriteState.Name}: Would you like me to record your deeds?", accepted =>
            {
                if (accepted)
                {
                    this.GameState.ReloadSaveGames();
                    var saveWindow = new SaveWindow(this._ui);
                    saveWindow.Show(this.GameState.GameSaveSlots.OrderByDescending(i => i.Time), save =>
                        {
                            if (save == null)
                            {
                                this.GameState.IsPaused = false;
                                return;
                            }

                            this.GameState.Save(save);
                            var levelText = party.AliveMembers.Aggregate("",
                                (current, member) =>
                                    current +
                                    $"{member.Name} needs {member.NextLevel - member.Xp} xp to get to next level\n");
                            new TalkWindow(this._ui).Show($"{this.SpriteState.Name}: It has been recorded\n{levelText}",
                                () => { this.GameState.IsPaused = false; });
                        }
                    );
                    
                }
                else
                {
                    this.GameState.IsPaused = false;
                }
            });
            
            return true;
        }
    }
}
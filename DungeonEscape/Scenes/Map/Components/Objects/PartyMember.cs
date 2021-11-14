namespace DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using Common.Components.UI;
    using Nez.AI.Pathfinding;
    using Nez.Tiled;
    using State;

    public class PartyMember : Sprite
    {
        private readonly UISystem ui;
        private string text;
        private string name;
        private Class memberClass;

        public PartyMember(TmxObject tmxObject, SpriteState state, TmxMap map, UISystem uiSystem, IGame gameState,
            AstarGridGraph graph) : base(tmxObject, state, map, gameState, graph)
        {
            this.text = tmxObject.Properties.ContainsKey("Text") ? tmxObject.Properties["Text"] : "";
            this.name = tmxObject.Name;
            this.memberClass = tmxObject.Properties.ContainsKey("Class") ? Enum.Parse<Class>(tmxObject.Properties["Class"]) : Class.Fighter;
            this.ui = uiSystem;
        }
        
        public override bool OnAction(Party party)
        {
            this.gameState.IsPaused = true;
            new QuestionWindow(this.ui).Show(this.text, join =>
            {
                if (join)
                {
                    var hero = new Hero
                    {
                        Name = this.name,
                        Class = this.memberClass
                    };
                
                    hero.RollStats(this.gameState.ClassLevelStats);
                    this.gameState.Party.Members.Add(hero);
                    
                    new TalkWindow(this.ui).Show($"{hero.Name} Joined the party", ()=>
                    {
                        this.gameState.IsPaused = false;
                    });
                }
                else
                {
                    this.gameState.IsPaused = false;
                }

            });
            return true;
        }
    }
}
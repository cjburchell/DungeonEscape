namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using System.Collections.Generic;
    using Common.Components.UI;
    using Nez.AI.Pathfinding;
    using Nez.Tiled;
    using State;

    public class PartyMember : Sprite
    {
        private readonly UiSystem _ui;
        private readonly string _text;
        private readonly string _name;
        private readonly Class _memberClass;
        private readonly Gender _gender;

        public PartyMember(TmxObject tmxObject, SpriteState state, TmxMap map, UiSystem uiSystem, IGame gameState,
            AstarGridGraph graph) : base(tmxObject, state, map, gameState, graph)
        {
            this._text = tmxObject.Properties.ContainsKey("Text") ? tmxObject.Properties["Text"] : "";
            this._name = tmxObject.Name;
            this._memberClass = tmxObject.Properties.ContainsKey("Class") ? Enum.Parse<Class>(tmxObject.Properties["Class"]) : Class.Fighter;
            this._gender = tmxObject.Properties.ContainsKey("Gender") ? Enum.Parse<Gender>(tmxObject.Properties["Gender"]) : Gender.Male;
            this._ui = uiSystem;
        }

        protected override void SetupAnimation(List<Nez.Textures.Sprite> sprites)
        {
            const int offset = 4;
            this.Animator.AddAnimation("WalkUp", new[]
            {
                sprites[this.BaseId + 0 - offset],
                sprites[this.BaseId + 1 - offset]
            });

            this.Animator.AddAnimation("WalkRight", new[]
            {
                sprites[this.BaseId + 2 - offset],
                sprites[this.BaseId + 3 - offset]
            });

            this.Animator.AddAnimation("WalkDown", new[]
            {
                sprites[this.BaseId + 4 - offset],
                sprites[this.BaseId + 5 - offset]
            });

            this.Animator.AddAnimation("WalkLeft", new[]
            {
                sprites[this.BaseId + 6 - offset],
                sprites[this.BaseId + 7 - offset]
            });
        }

        public override bool OnAction(Party party)
        {
            this.GameState.IsPaused = true;
            new QuestionWindow(this._ui).Show(this._text, join =>
            {
                if (join)
                {
                    var hero = new Hero
                    {
                        Name = this._name,
                        Class = this._memberClass,
                        Gender = this._gender
                    };
                
                    hero.RollStats(this.GameState.ClassLevelStats);
                    this.GameState.Party.Members.Add(hero);
                    
                    new TalkWindow(this._ui).Show($"{hero.Name} Joined the party", ()=>
                    {
                        this.GameState.IsPaused = false;
                    });
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
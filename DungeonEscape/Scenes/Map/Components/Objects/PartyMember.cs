namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.Components.UI;
    using Nez.AI.Pathfinding;
    using Nez.Tiled;
    using State;

    public class PartyMember : Character
    {
        private readonly Class _memberClass;
        private readonly Gender _gender;
        private readonly int _level;

        public PartyMember(TmxObject tmxObject, SpriteState state, TmxMap map, UiSystem uiSystem, IGame gameState,
            AstarGridGraph graph) : base(tmxObject, state, map, uiSystem, gameState, graph)
        {
            this._gender = tmxObject.Properties.ContainsKey("Gender") ? Enum.Parse<Gender>(tmxObject.Properties["Gender"]) : Gender.Male;
            if (string.IsNullOrEmpty(this.SpriteState.Name))
            {
                this.SpriteState.Name = tmxObject.Name;
            }
            
            if ((tmxObject.Name == "#Random#" && this.SpriteState.Name == "#Random#" ) || string.IsNullOrEmpty(this.SpriteState.Name))
            {
                do
                {
                    this.SpriteState.Name = gameState.GenerateName(this._gender);
                } while (gameState.Party.Members.Any(i => i.Name == this.SpriteState.Name));
            }
            
            this._memberClass = tmxObject.Properties.ContainsKey("Class") ? Enum.Parse<Class>(tmxObject.Properties["Class"]) : Class.Fighter;
            this._level = tmxObject.Properties.ContainsKey("Level") ? int.Parse(tmxObject.Properties["Level"]) : 1;

            var text = tmxObject.Properties.ContainsKey("Text") ? tmxObject.Properties["Text"] : null;
            if (!string.IsNullOrEmpty(text))
            {
                this.Dialog = new Dialog
                {
                    Dialogs = new List<DialogHead>
                    {
                        new()
                        {
                            Text = text,
                            Choices = new List<Choice>
                            {
                                new()
                                {
                                    Text = "Yes", Actions = new List<QuestAction> { QuestAction.Join }
                                },
                                new() {Text = "No"}
                            }
                        }
                    }
                };
            }
        }

        protected override bool JoinParty()
        {
            var hero = new Hero
            {
                Name = this.SpriteState.Name,
                Class = this._memberClass,
                Gender = this._gender,
                IsActive = false,
                Order = 0,
            };
                
            hero.Setup(this.GameState, this._level);
            var texture = this.Entity.Scene.Content.LoadTexture("Content/images/sprites/hero.png");
            hero.SetupImage(texture);

            var maxOrder = this.GameState.Party.ActiveMembers.Max(i => i.Order);
            
            this.Entity.SetEnabled(false);
            this.Entity.Scene.Entities.Remove(this.Entity);
            this.SpriteState.IsActive = false;
            
            this.GameState.Party.Members.Add(hero);
            if (this.GameState.Party.ActiveMembers.Count() >= this.GameState.Settings.MaxPartyMembers)
            {
                return false;
            }
            
            hero.IsActive = true;
            hero.Order = maxOrder + 1;
            return true;
        }
    }
}
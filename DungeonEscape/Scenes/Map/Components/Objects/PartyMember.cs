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
                    this.SpriteState.Name = this._gender == Gender.Male
                        ? gameState.Names.Male[Nez.Random.NextInt(gameState.Names.Male.Count)]
                        : gameState.Names.Female[Nez.Random.NextInt(gameState.Names.Male.Count)];
                } while (gameState.Party.Members.Any(i => i.Name == this.SpriteState.Name));
            }
            
            this._memberClass = tmxObject.Properties.ContainsKey("Class") ? Enum.Parse<Class>(tmxObject.Properties["Class"]) : Class.Fighter;
            this._level = tmxObject.Properties.ContainsKey("Level") ? int.Parse(tmxObject.Properties["Level"]) : 1;

            var text = tmxObject.Properties.ContainsKey("Text") ? tmxObject.Properties["Text"] : null;
            if (!string.IsNullOrEmpty(text))
            {
                this.Dialog = new()
                {
                    Dialogs = new()
                    {
                        new()
                        {
                            Text = $"{this.SpriteState.Name}: {text}",
                            Choices = new List<Choice>
                            {
                                new()
                                {
                                    Text = "Yes", Action = QuestAction.Join
                                },
                                new() {Text = "No"}
                            }
                        }
                    }
                };
            }
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

        protected override void JoinParty()
        {
            var hero = new Hero
            {
                Name = this.SpriteState.Name,
                Class = this._memberClass,
                Gender = this._gender
            };
                
            hero.Setup(this.GameState, this._level);
                    
            var lastMember = this.GameState.Party.Members.Last();
            this.GameState.Party.Members.Add(hero);
            this.SpriteState.IsActive = false;

            this.Entity.SetEnabled(false);
            this.Entity.Scene.Entities.Remove(this.Entity);
            
            var texture = this.Entity.Scene.Content.LoadTexture("Content/images/sprites/hero.png");
            hero.SetupImage(texture);
            var lastEntity = this.Entity.Scene.FindEntity(lastMember.Id);
            var player = this.Entity.Scene.FindEntity(this.GameState.Party.Members.First().Id).GetComponent<PlayerComponent>();
            var followerEntity = this.Entity.Scene.CreateEntity(hero.Id, this.Entity.Position);
            followerEntity.AddComponent(new Follower( hero, lastEntity, player, this.GameState));
        }
    }
}
namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;
    using Nez;
    using Nez.Sprites;
    using Nez.Tiled;
    using Nez.UI;
    using State;
    using Random = Nez.Random;

    public class PlayerComponent : Component, IUpdatable, ITriggerListener, IPlayer
    {
        private readonly TmxMap _map;
        private readonly Label _debugText;
        private readonly List<RandomMonster> _randomMonsters;
        private readonly UiSystem _ui;
        private readonly int _renderOffset;
        private const float MoveSpeed = 150;
        private SpriteAnimator _playerAnimation;
        private SpriteAnimator _shipAnimator;
        private VirtualIntegerAxis _xAxisInput;
        private VirtualIntegerAxis _yAxisInput;
        private readonly IGame _gameState;
        public List<ICollidable> CurrentlyOverObjects { get; } = new();
        private PartyStatusWindow _statusWindow;
        private GoldWindow _goldWindow;
        private Mover _mover;
        private VirtualButton _actionButton;
        private float _distance;

        private SpriteAnimator _animator;
        private List<Nez.Textures.Sprite> _sprites;
        private Hero _lastHero;

        public PlayerComponent(IGame gameState, TmxMap map, Label debugText, List<RandomMonster> randomMonsters, UiSystem ui, int renderOffset)
        {
            this._map = map;
            this._debugText = debugText;
            this._randomMonsters = randomMonsters;
            this._ui = ui;
            _renderOffset = renderOffset;
            this._gameState = gameState;
        }

        private void UpdateAnimation()
        {
            var hero = this._gameState.Party.GetOrderedHero(0);
            if (_lastHero == hero)
            {
                return;    
            }

            _lastHero = hero;
            
            var animationBaseIndex = (int) hero.Class * 16 + (int) hero.Gender * 8;
            _animator.AddAnimation("WalkDown", new[]
            {
                _sprites[animationBaseIndex + 4],
                _sprites[animationBaseIndex + 5]
            });

            _animator.AddAnimation("WalkUp", new[]
            {
                _sprites[animationBaseIndex + 0],
                _sprites[animationBaseIndex + 1]
            });

            _animator.AddAnimation("WalkRight", new[]
            {
                _sprites[animationBaseIndex + 2],
                _sprites[animationBaseIndex + 3]
            });

            _animator.AddAnimation("WalkLeft", new[]
            {
                _sprites[animationBaseIndex + 6],
                _sprites[animationBaseIndex + 7]
            });

            _animator.SetSprite(_sprites[animationBaseIndex + 4]);
        }
        
        public override void OnAddedToEntity()
        {
            const int heroHeight = 48;
            const int heroWidth = MapScene.DefaultTileSize;
            this._mover = this.Entity.AddComponent(new Mover());
            {
                var texture = this.Entity.Scene.Content.LoadTexture("Content/images/sprites/hero.png");
                _sprites = Nez.Textures.Sprite.SpritesFromAtlas(texture, heroWidth, heroHeight);

                var hero = this._gameState.Party.GetOrderedHero(0);
                var animationBaseIndex = (int) hero.Class * 16 + (int) hero.Gender * 8;
                _animator = this.Entity.AddComponent(new SpriteAnimator(_sprites[animationBaseIndex + 4]));
                _animator.Speed = 0.5f;
                
                const int deadAnimationIndex = 144;
                UpdateAnimation();
            
                _animator.AddAnimation("WalkDownDead", new[]
                {
                    _sprites[deadAnimationIndex + 4],
                    _sprites[deadAnimationIndex + 5]
                });

                _animator.AddAnimation("WalkUpDead", new[]
                {
                    _sprites[deadAnimationIndex + 0],
                    _sprites[deadAnimationIndex + 1]
                });

                _animator.AddAnimation("WalkRightDead", new[]
                {
                    _sprites[deadAnimationIndex + 2],
                    _sprites[deadAnimationIndex + 3]
                });

                _animator.AddAnimation("WalkLeftDead", new[]
                {
                    _sprites[deadAnimationIndex + 6],
                    _sprites[deadAnimationIndex + 7]
                });

                this._playerAnimation = _animator;
                this._playerAnimation.SetEnabled(false);
            }

            {
                var texture = this.Entity.Scene.Content.LoadTexture("Content/images/sprites/ship2.png");
                var sprites = Nez.Textures.Sprite.SpritesFromAtlas(texture, heroWidth, 56);
                this._shipAnimator = this.Entity.AddComponent(new SpriteAnimator(sprites[0]));
                this._shipAnimator.Speed = 0.5f;
                this._shipAnimator.AddAnimation("WalkDown", new[]
                {
                    sprites[0],
                    sprites[1]
                });

                this._shipAnimator.AddAnimation("WalkUp", new[]
                {
                    sprites[4],
                    sprites[5]
                });

                this._shipAnimator.AddAnimation("WalkRight", new[]
                {
                    sprites[2],
                    sprites[3]
                });

                this._shipAnimator.AddAnimation("WalkLeft", new[]
                {
                    sprites[6],
                    sprites[7]
                });
            }
            
            const int a = heroHeight/2 - heroWidth/2; // 16

            var box = new Rectangle
            {
                X = -(heroWidth / 4),
                Y = a-heroWidth / 4,
                Width = heroWidth / 2,
                Height = heroWidth / 2
            };

            this.Entity.AddComponent(new BoxCollider(box));

            this._xAxisInput = new VirtualIntegerAxis();
            this._xAxisInput.Nodes.Add(new VirtualAxis.GamePadDpadLeftRight());
            this._xAxisInput.Nodes.Add(new VirtualAxis.GamePadLeftStickX());
            this._xAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.Left,
                Keys.Right));
            this._xAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.A,
                Keys.D));

            this._yAxisInput = new VirtualIntegerAxis();
            this._yAxisInput.Nodes.Add(new VirtualAxis.GamePadDpadUpDown());
            this._yAxisInput.Nodes.Add(new VirtualAxis.GamePadLeftStickY());
            this._yAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.Up,
                Keys.Down));
            this._yAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.W,
                Keys.S));

            this._actionButton = new VirtualButton();
            this._actionButton.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Space));
            this._actionButton.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.A));

            this._statusWindow =
                new PartyStatusWindow(this._gameState.Party, this._ui.Canvas, this._ui.Sounds);

            this._goldWindow =
                new GoldWindow(this._gameState.Party, this._ui.Canvas, this._ui.Sounds);

            var overWater = this.IsOverWater();
            this._shipAnimator.SetEnabled(overWater);
            this._playerAnimation.SetEnabled(!overWater);
            _shipAnimator.RenderLayer = (int)(_renderOffset - this.Entity.Position.Y + 12);
            _playerAnimation.RenderLayer = (int)(_renderOffset - this.Entity.Position.Y + 12);

            //this.Entity.AddComponent(new SpriteTrail());
        }

        public override void OnRemovedFromEntity()
        {
            this._xAxisInput.Deregister();
            this._yAxisInput.Deregister();
            this._actionButton.Deregister();
        }

        public bool IsOverWater()
        {
            var (x, y) = MapScene.ToMapGrid(this.Entity.Position, this._map);
            var tile = this._map.GetLayer<TmxLayer>("water")?.GetTile(x, y);
            return tile != null && tile.Gid != 0;
        }

        private bool UpdateMovement()
        {
            var overWater = this.IsOverWater();
            this._shipAnimator.SetEnabled(overWater);
            this._playerAnimation.SetEnabled(!overWater);
            // handle movement and animations    
            var moveDir = new Vector2(this._xAxisInput.Value, this._yAxisInput.Value);

            if (moveDir == Vector2.Zero)
            {
                this._shipAnimator.Pause();
                this._playerAnimation.Pause();
                return false;
            }

            if (Math.Abs(moveDir.X) > Math.Abs(moveDir.Y))
            {
                moveDir.Y = 0;
            }
            else
            {
                moveDir.X = 0;
            }

            var animation = "WalkDown";
            if (moveDir.X < 0)
            {
                animation = "WalkLeft";
            }
            else if (moveDir.X > 0)
            {
                animation = "WalkRight";
            }

            if (moveDir.Y < 0)
            {
                animation = "WalkUp";
            }
            else if (moveDir.Y > 0)
            {
                animation = "WalkDown";
            }

            var waterAnimation = animation;
            var hero = this._gameState.Party.GetOrderedHero(0);
            if (hero.IsDead)
            {
                animation += "Dead";
            }
            
            var movement = moveDir * (overWater?MoveSpeed*1.5f:MoveSpeed) * Time.DeltaTime;
            var newPoint = movement + this.Entity.Position;

            var minX = this._map.TileWidth / 2.0f;
            var maxX = this._map.Width * this._map.TileWidth - this._map.TileWidth/2.0f;
                
            if (newPoint.X < minX)
            {
                newPoint.X = !this._gameState.Party.CurrentMapIsOverWorld? minX : maxX;
            }
            else if (newPoint.X > maxX)
            {
                newPoint.X = !this._gameState.Party.CurrentMapIsOverWorld ? maxX : minX;
            }

            var minY = this._map.TileHeight / 2.0f;
            var maxY = this._map.Height * this._map.TileHeight - this._map.TileHeight/2.0f;
                
            if (newPoint.Y < minY)
            {
                newPoint.Y = !this._gameState.Party.CurrentMapIsOverWorld ? minY : maxY;
            }
            else if (newPoint.Y > maxY)
            {
                newPoint.Y = !this._gameState.Party.CurrentMapIsOverWorld ? maxY : minY;
            }

            movement = newPoint - this.Entity.Position;

            this._gameState.Party.CurrentPosition = this.Entity.Position;
            if (this._gameState.Party.CurrentMapIsOverWorld)
            {
                this._gameState.Party.OverWorldPosition = this._gameState.Party.CurrentPosition.Value;
            }

            if (! this._playerAnimation.IsAnimationActive(animation))
            {
                this._playerAnimation.Play(animation);
            }
            else
            {
                this._playerAnimation.UnPause();
            }

            if (!this._shipAnimator.IsAnimationActive(waterAnimation))
            {
                this._shipAnimator.Play(waterAnimation);
            }
            else
            {
                this._shipAnimator.UnPause();
            }

            if (this._mover.CalculateMovement(ref movement, out _))
            {
                return false;
            }

            this._distance += movement.Length();
            this._mover.ApplyMovement(movement);
            
            if (_playerAnimation is { IsVisible: true })
            {
                _playerAnimation.RenderLayer = (int)(_renderOffset - this.Entity.Position.Y + 12);
            }
            
            if (_shipAnimator is { IsVisible: true })
            {
                _shipAnimator.RenderLayer = (int)(_renderOffset - this.Entity.Position.Y + 12);
            }
            
            return true;
        }

        void IUpdatable.Update()
        {
            UpdateAnimation();
            if (this._gameState.IsPaused)
            {
                this._gameState.UpdatePauseState();
                this._statusWindow.CloseWindow(false);
                this._goldWindow.CloseWindow(false);
                return;
            }

            if (this._debugText.IsVisible())
            {
                var currentBiome = MapScene.GetCurrentBiome(this._map, this.Entity.Position);

                var objectList = CurrentlyOverObjects.Aggregate("", (current, overObject) => current + $"{overObject.State.Name},");

                this._debugText.SetText(
                    $"B: {currentBiome.Type}{currentBiome.MaxMonsterLevel}, G: {MapScene.ToMapGrid(this.Entity.Position, this._map)}, R: {this.Entity.Position.X:F2}:{this.Entity.Position.Y:F2}  d: {this._distance:F2} over: {objectList}");
            }
            
            var actionItems = this.CurrentlyOverObjects.Where(i=> i.CanDoAction()).ToList();
            if (this._actionButton.IsReleased && actionItems.Any())
            {
                void Done()
                {
                    this._gameState.IsPaused = false;
                }
                this._gameState.IsPaused = true;
                
                if (actionItems.Count == 1)
                {
                    actionItems.First().OnAction(Done);
                }
                else
                {
                    new SelectWindow<string>(this._ui, null, new Point(20, 20)).Show(actionItems.Select(i => i.State.Name),
                        selection =>
                        {
                            if (selection == null)
                            {
                                this._gameState.IsPaused = false;
                                return;
                            }

                            var overObject = actionItems.FirstOrDefault(i => i.State.Name == selection);
                            overObject?.OnAction(Done);
                        });
                }
            }

            if (this._gameState.IsPaused)
            {
                this._statusWindow.CloseWindow(false);
                this._goldWindow.CloseWindow(false);
                return;
            }

            if (!this.UpdateMovement())
            {
                this._statusWindow.ShowWindow();
                this._goldWindow.ShowWindow();
                return;
            }

            this._statusWindow.CloseWindow(false);
            this._goldWindow.CloseWindow(false);

            if (this._gameState.IsPaused)
            {
                return;
            }

            if (!this.CheckForFullStep())
            {
                return;
            }

            if (this.CheckForMonsterEncounter())
            {
                this.DoMonsterEncounter();
                return;
            }
            
            var message = this.CheckStatusEffects();
            message += this.CheckDamageTile();
            

            if (!string.IsNullOrWhiteSpace(message))
            {
                this._gameState.IsPaused = true;
                new TalkWindow(this._ui).Show(message, () =>
                {
                    this._gameState.IsPaused = false;
                    if (!this._gameState.Party.AliveMembers.Any())
                    {
                        this._gameState.ShowMainMenu();
                    }
                });
                return;
            }

            if (!this._gameState.Party.AliveMembers.Any())
            {
                this._gameState.ShowMainMenu();
            }
        }

        private string CheckDamageTile()
        {
            var (x, y) = MapScene.ToMapGrid(this.Entity.Position, this._map);
            var tile = this._map.GetLayer<TmxLayer>("damage")?.GetTile(x, y);
            if (tile == null || tile.Gid == 0 || !tile.TilesetTile.Properties.ContainsKey("damage"))
            {
                return "";
            }

            var message = "";
            var damage = int.Parse(tile.TilesetTile.Properties["damage"]);
            foreach (var member in this._gameState.Party.AliveMembers)
            {
                member.Health -= damage;
                this._gameState.Sounds.PlaySoundEffect("receive-damage");
                if (!member.IsDead)
                {
                    continue;
                }

                message += $"{member.Name} has died!\n";
                member.Health = 0;
            }

            return message;
        }

        private string CheckStatusEffects()
        {
            var message = "";
            foreach (var member in this._gameState.Party.AliveMembers)
            {
                message += member.CheckForExpiredStates(this._gameState.Party.StepCount, DurationType.Distance);
                member.UpdateStatusEffects(this._gameState);
                if (member.IsDead)
                {
                    message += $"{member.Name} has died!\n"; 
                }
            }

            return message;
        }

        private void DoMonsterEncounter()
        {
            var currentBiome = MapScene.GetCurrentBiome(this._map, this.Entity.Position);
            var availableMonsters = new List<Monster>();

            var level = this._gameState.Party.MaxLevel();

            var monsterList = this._randomMonsters.Where(item => item.InBiome(currentBiome.Type) &&
                                                                 (currentBiome.MaxMonsterLevel == 0 ||
                                                                  item.Data.MinLevel < currentBiome.MaxMonsterLevel) &&
                                                                 item.Data.MinLevel >= currentBiome.MinMonsterLevel);

            foreach (var monster in monsterList)
            {
                var probability = monster.Rarity switch
                {
                    Rarity.Common => 20,
                    Rarity.Uncommon => 5,
                    Rarity.Rare => 2,
                    Rarity.Epic => 1,
                    Rarity.Legendary => Dice.RollD20() > 14 ? 1 : 0 ,
                    _ => throw new ArgumentOutOfRangeException()
                };

                for (var i = 0; i < probability; i++)
                {
                    availableMonsters.Add(monster.Data);
                }
            }

            if (availableMonsters.Count == 0)
            {
                return;
            }

            const int maxMonstersToFight = 10;
            const int maxMonsterGroups = 3;
            var maxMonsters = level / 4 + this._gameState.Party.AliveMembers.Count();
            if (maxMonsters > maxMonstersToFight)
            {
                maxMonsters = maxMonstersToFight;
            }

            var numberOfMonsters = Random.NextInt(maxMonsters) + 1;
            var monsters = new List<Monster>();
            var totalMonsters = 0;
            var usedMonsters = new List<string>();
            for (var group = 0; group < maxMonsterGroups - 1; group++)
            {
                var availableMonstersList = availableMonsters.Where(i => !usedMonsters.Contains(i.Name)).ToArray();
                if (!availableMonsters.Any())
                {
                    break;
                }
                
                var monsterNub = Random.NextInt(availableMonstersList.Length);
                var monster = availableMonstersList[monsterNub];
                usedMonsters.Add(monster.Name);
                var numberInGroup = Random.NextInt(Math.Min(numberOfMonsters - totalMonsters, monster.GroupSize)) + 1;
                for (var i = 0; i < numberInGroup; i++)
                {
                    monsters.Add(monster);
                }

                totalMonsters += numberInGroup;
                if (totalMonsters >= numberOfMonsters)
                {
                    break;
                }
            }

            if (totalMonsters < numberOfMonsters)
            {
                var availableMonstersList = availableMonsters.Where(i => !usedMonsters.Contains(i.Name)).ToArray();
                if (availableMonsters.Any())
                {
                    var monsterNub = Random.NextInt(availableMonstersList.Length);
                    var monster = availableMonstersList[monsterNub];
                    var numberInGroup = Math.Min(numberOfMonsters - totalMonsters, monster.GroupSize);
                    for (var i = 0; i < numberInGroup; i++)
                    {
                        monsters.Add(monster);
                    }
                }
            }

            var repelActive =
                this._gameState.Party.AliveMembers.Any(
                    partyMember => partyMember.Status.Any(i => i.Type == EffectType.Repel));
            if (repelActive)
            {
                var maxHealth = this._gameState.Party.AliveMembers.Max(i => i.MaxHealth);
                foreach (var monster in monsters.ToList())
                {
                    var monsterHealth = Dice.Roll(monster.HealthRandom, monster.HealthTimes, monster.HealthConst);
                    if (monsterHealth < maxHealth)
                    {
                        monsters.Remove(monster);
                    }
                }
            }
            
            if (!monsters.Any())
            {
                return;
            }

            this._gameState.StartFight(monsters, currentBiome.Type);
        }

        private bool CheckForFullStep()
        {
            if (this._distance <= this._map.TileWidth)
            {
                return false;
            }

            this._gameState.Party.StepCount++;
            this._distance = 0;
            return true;
        }

        private bool CheckForMonsterEncounter()
        {
            if (this._gameState.Settings.NoMonsters)
            {
                return false;
            }
            
            
            var currentBiome = MapScene.GetCurrentBiome(this._map, this.Entity.Position).Type;
            var hasRandomMonsters = this._randomMonsters.Any(item => item.InBiome(currentBiome));
            return  hasRandomMonsters && Random.Chance(0.1f);
        }
        
        public void OnTriggerEnter(Collider other, Collider local)
        {
            if (this._gameState.IsPaused)
            {
                return;
            }

            if (other is not ObjectBoxCollider objCollider)
            {
                return;
            }

            if (!this.CurrentlyOverObjects.Contains(objCollider.Object))
            {
                this.CurrentlyOverObjects.Add(objCollider.Object);
            }
            
            objCollider.Object.OnHit();
        }

        public void OnTriggerExit(Collider other, Collider local)
        {
            if (this._gameState.IsPaused)
            {
                return;
            }

            if (other is not ObjectBoxCollider objCollider)
            {
                return;
            }
            
            this.CurrentlyOverObjects.Remove(objCollider.Object);
        }
    }
}    
namespace Redpoint.DungeonEscape.Scenes.Fight
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.Components.UI;
    using Map;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;
    using State;
    using Random = Nez.Random;

    public class FightScene : Scene
    {

        private enum RoundActionState
        {
            Run,
            Fight,
            Spell,
            Item,
            Nothing,
        }
        
        private class RoundAction
        {
            public IFighter Source { get; init; }
            public RoundActionState State { get; init; }
            public Spell Spell { get; init; }
            public ItemInstance Item { get; init; }
            public IEnumerable<IFighter> Targets { get; set; }
        }

        private enum EncounterRoundState
        {
            Begin,
            StartRound,
            ChooseAction,
            ChoosingAction,
            EndRound,
            EndEncounter,
            DoingActions,
            StartDoingActions
        }
        
        private readonly IGame _game;
        private readonly Biome _biome;
        private readonly List<MonsterInstance> _monsters = new List<MonsterInstance>();
        private UiSystem _ui;
        private EncounterRoundState _state = EncounterRoundState.Begin;
        private readonly List<RoundAction> _roundActions = new List<RoundAction>();
        private List<Hero> _heroes;
        private int _round;

        public FightScene(IGame game, IEnumerable<Monster> monsters, Biome biome)
        {
            this._game = game;
            this._biome = biome;
            foreach (var monster in monsters)
            {
                this._monsters.Add(new MonsterInstance(monster));
            }
        }
        
        // list of songs that can play during a battle
        private static readonly List<string> Songs = new List<string>
            { "battleground", "like-totally-rad", "sword-metal", "unprepared"};

        private const string EndFightSong = "not-in-vain";

        private Image GetBackgroundImage(Biome biome)
        {
            var imageFile = biome switch
            {
                Biome.Grassland => "field",
                Biome.Forest => "field",
                Biome.Water => "ocean",
                Biome.Hills => "mountain",
                Biome.Desert => "desert",
                Biome.Swamp => "swamp",
                Biome.Cave => "cave",
                Biome.Town => "castle",
                Biome.Tower => "tower",
                _ => "field"
            };

            var texture = this.Content.LoadTexture($"Content/images/background/{imageFile}.png");
            return new Image(texture, Scaling.None);
        }
        
        public override void Initialize()
        {
            base.Initialize();

            this.ClearColor = Color.Black;
            this.SetDesignResolution(MapScene.ScreenWidth, MapScene.ScreenHeight,
                MapScene.SceneResolution);

            this.AddRenderer(new DefaultRenderer());
            this._ui = new UiSystem(this.CreateEntity("ui-canvas").AddComponent(new UICanvas()), this._game.Sounds);
            this._ui.Canvas.SetRenderLayer(999);
            this._ui.Canvas.Stage.GamepadActionButton = null;

            var background = this._ui.Canvas.Stage.AddElement(this.GetBackgroundImage(this._biome));
            background.SetWidth(MapScene.ScreenWidth);
            background.SetHeight(MapScene.ScreenHeight * 2.0f / 3.0f);
            
            var table = this._ui.Canvas.Stage.AddElement(new Table());
            table.SetFillParent(true);
            table.Center();
            foreach (var monster in this._monsters)
            {
                table.Add(monster.Image).Pad(10);
            }
            
            var partyWindow = new PartyStatusWindow(this._game.Party, this._ui.Canvas, this._game.Sounds);
            partyWindow.ShowWindow();

            var monsterName = this._monsters.Count == 1 ?$"a {this._monsters.First().Name}"  : $"{this._monsters.Count} enemies";
            var message =$"You have encountered {monsterName}!";
            
            new FightTalkWindow(this._ui, "").Show(message, ()=> this._state = EncounterRoundState.StartRound);
            
            this._game.Sounds.PlayMusic(Songs[Random.NextInt(Songs.Count)]);
        }

        public override void Update()
        {
            foreach (var monster in this._monsters)
            {
                monster.Update();
            }
            
            foreach (var member in this._game.Party.Members)
            {
                member.Update();
            }
            
            base.Update();
            switch (this._state)
            {
                case EncounterRoundState.Begin:
                    break;
                case EncounterRoundState.StartRound:
                    this.StartRound();
                    break;
                case EncounterRoundState.ChooseAction:
                    this.ChoosingActions();
                    break;
                case EncounterRoundState.ChoosingAction:
                    break;
                case EncounterRoundState.StartDoingActions:
                    this.DoActions();
                    break;
                case EncounterRoundState.DoingActions:
                    break;
                case EncounterRoundState.EndRound:
                    this.EndRound();
                    break;
                case EncounterRoundState.EndEncounter:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private void StartRound()
        {
            this._round++;
            this._roundActions.Clear();
            this._heroes = this._game.Party.Members.ToList();
            foreach (var monster in this._monsters.Where(item=> !item.IsDead && !item.RanAway))
            {
                var action = this.ChooseAction(monster);
                this._roundActions.Add(action);
            }
            this._state = EncounterRoundState.ChooseAction;
        }

        private void ChoosingActions()
        {
            var nextHero = this._heroes.FirstOrDefault(member => !member.IsDead);
            if (nextHero == null)
            {
                this.OrderActions();
                this._state = EncounterRoundState.StartDoingActions;
            }
            else
            {
                this._state = EncounterRoundState.ChoosingAction;
                this.ChooseAction(nextHero, action =>
                {
                    this._state = EncounterRoundState.ChooseAction;
                    if (action == null)
                    {
                        return;
                    }

                    this._heroes.Remove(nextHero);
                    this._roundActions.Add(action);
                });
            }
        }

        private static bool CanBeAttacked(IFighter fighter)
        {
            return !fighter.IsDead && !fighter.RanAway;
        }

        private void EndRound()
        {
            if (this._game.Party.Members.Count(CanBeAttacked) != 0 &&
                this._monsters.Count(CanBeAttacked) != 0)
            {
                this._state = EncounterRoundState.StartRound;
                return;
            }

            this._state = EncounterRoundState.EndEncounter;
            this.EndEncounter();
        }

        private void ChooseAction(IFighter hero, Action<RoundAction> done)
        {
            if (hero.Status.Count(i => i.Type == EffectType.Sleep) != 0)
            {
                done(new RoundAction
                {
                    Source = hero,
                    State = RoundActionState.Nothing
                });
                
                return;
            }
            
            if (hero.Status.Count(i => i.Type == EffectType.Confusion) != 0)
            {
                done(this.ChooseAction(hero));
                return;
            }
            
            var selectAction =
                new SelectWindow<string>(this._ui, hero.Name, new Point(10, MapScene.ScreenHeight / 3 * 2));
            
            var options = new List<string> {"Fight"};
            var spells = hero.GetSpells(this._game.Spells).ToList();
            var availableSpells = spells.Where(item => item.IsEncounterSpell && item.Cost <= hero.Magic).ToList();
            if (availableSpells.Count != 0 && hero.Status.Count(i => i.Type == EffectType.StopSpell) == 0)
            {
                options.Add("Spell");
            }
            
            var availableItems = this._game.Party.Items.Where(item => item.Type == ItemType.OneUse).ToList();
            if (availableItems.Count != 0)
            {
                options.Add("Item");
            }
            
            options.Add("Run");
            
            selectAction.Show(options, selection =>
            {
                if (selection == null)
                {
                    done(null);
                    return;
                }
                
                switch (selection)
                {
                    case "Fight":
                    {
                        this.ChooseFight(hero, done);
                        return;
                    }
                    case "Spell":
                    { 
                        this.ChooseSpell(hero, availableSpells, done);
                        return;
                    }
                    case "Item":
                    {
                        this.ChooseItem(hero, availableItems, done);
                        return;
                    }
                    case "Run":
                    {
                        ChooseRun(hero, done);
                        return;
                    }
                }

            });
        }

        private static void ChooseRun(IFighter hero, Action<RoundAction> done)
        {
            done(new RoundAction
            {
                Source = hero,
                State = RoundActionState.Run
            });
        }

        private void ChooseItem(IFighter hero, IEnumerable<ItemInstance> availableItems, Action<RoundAction> done)
        {
            var selectItem = new InventoryWindow(this._ui, new Point(10, MapScene.ScreenHeight / 3 * 2));
            selectItem.Show(availableItems, item =>
            {
                if (item == null)
                {
                    done(null);
                    return;
                }

                if (this._game.Party.Members.Count(member => !member.IsDead) == 1)
                {
                    var newAction = new RoundAction
                    {
                        Source = hero,
                        State = RoundActionState.Item,
                        Targets = new []{hero},
                        Item = item
                    };

                    done(newAction);
                    return;
                }

                var selectTarget = new SelectHeroWindow(this._ui,
                    new Point(10, MapScene.ScreenHeight / 3 * 2));
                selectTarget.Show(this._game.Party.Members.Where(member => !member.IsDead), target =>
                {
                    if (target == null)
                    {
                        done(null);
                        return;
                    }

                    var newAction = new RoundAction
                    {
                        Source = hero,
                        State = RoundActionState.Item,
                        Targets = new []{target},
                        Item = item
                    };

                    done(newAction);
                });
            });
        }

        private void ChooseSpell(IFighter hero, IEnumerable<Spell> availableSpells, Action<RoundAction> done)
        {
            var selectItem = new SpellWindow(this._ui, new Point(10, MapScene.ScreenHeight / 3 * 2), hero);
            selectItem.Show(availableSpells, spell =>
            {
                if (spell == null)
                {
                    done(null);
                    return;
                }

                var newAction = new RoundAction
                {
                    Source = hero,
                    State = RoundActionState.Spell,
                    Spell = spell
                };

                if (spell.IsAttackSpell)
                {
                    if (spell.Targets == Target.Single)
                    {
                        var selectTarget = new SelectWindow<MonsterInstance>(this._ui, "SelectMonster",
                            new Point(10, MapScene.ScreenHeight / 3 * 2), 250);
                        selectTarget.Show(this._monsters.Where(item => !item.IsDead), monster =>
                        {
                            if (monster == null)
                            {
                                done(null);
                                return;
                            }

                            newAction.Targets = new[] {monster};
                            done(newAction);
                        });
                        return;
                    }

                    newAction.Targets = this._monsters.Where(item => !item.IsDead);
                    done(newAction);
                    return;

                }

                if (spell.Targets == Target.Single)
                {
                    if (this._game.Party.Members.Count(member => !member.IsDead) == 1)
                    {
                        newAction.Targets = new[] {hero};
                        done(newAction);
                        return;
                    }

                    var selectTarget = new SelectHeroWindow(this._ui,
                        new Point(10, MapScene.ScreenHeight / 3 * 2));
                    selectTarget.Show(this._game.Party.Members.Where(member => !member.IsDead), target =>
                    {
                        if (target == null)
                        {
                            done(null);
                            return;
                        }

                        newAction.Targets = new[] {target};
                        done(newAction);
                    });
                    return;
                }

                newAction.Targets = this._game.Party.Members.Where(item => !item.IsDead);
                done(newAction);
            });
        }

        private void ChooseFight(IFighter hero, Action<RoundAction> done)
        {
            var selectTarget = new SelectWindow<MonsterInstance>(this._ui, "SelectMonster",
                new Point(10, MapScene.ScreenHeight / 3 * 2), 250);
            selectTarget.Show(this._monsters.Where(item => !item.IsDead), monster =>
            {
                if (monster == null)
                {
                    done(null);
                    return;
                }

                var newAction = new RoundAction
                {
                    Source = hero,
                    State = RoundActionState.Fight,
                    Targets = new []{monster}
                };

                done(newAction);
            });
        }

        private RoundAction ChooseAction(IFighter fighter)
        {
            if (fighter.Status.Count(i => i.Type == EffectType.Sleep) != 0)
            {
                return new RoundAction
                {
                    Source = fighter,
                    State = RoundActionState.Nothing
                };
            }

            var availableTargets = this._game.Party.Members.OfType<IFighter>().Where(CanBeAttacked).ToList();
            if (fighter.Status.Count(i => i.Type == EffectType.Confusion) != 0)
            {
                availableTargets.AddRange(this._monsters.Where(CanBeAttacked));
            }

            var target = availableTargets.ToArray()[Random.NextInt(availableTargets.Count)];
            
            if (fighter.Status.Count(i => i.Type == EffectType.StopSpell) == 0)
            {
                // check to cast heal spell
                if ( fighter.GetSpells(this._game.Spells).Count(item => item.Type == SpellType.Heal && item.Cost <= fighter.Magic) != 0 
                     && (float) fighter.Health / fighter.MaxHealth * 100f < 10)
                {
                    var healSpells =
                        fighter.GetSpells(this._game.Spells).Where(item => item.Type == SpellType.Heal && item.Cost <= fighter.Magic)
                            .ToArray();

                    var spell = healSpells[Random.NextInt(healSpells.Length)];
                    return new RoundAction
                    {
                        Source = fighter,
                        State = RoundActionState.Spell,
                        Spell = spell
                    };
                }

                // check to cast offencive spells
                if (fighter.GetSpells(this._game.Spells).Count(item => item.IsAttackSpell && item.Cost <= fighter.Magic) != 0) // has spells
                {
                    var attackSpells =
                        fighter.GetSpells(this._game.Spells).Where(item => item.IsAttackSpell && item.Cost <= fighter.Magic).ToArray();

                    var spell = attackSpells[Random.NextInt(attackSpells.Length)];

                    var targets = spell.Targets == Target.Group
                        ? this._game.Party.Members.Where(CanBeAttacked).OfType<IFighter>()
                        : new List<IFighter>
                        {
                            target
                        };
                    
                    return new RoundAction
                    {
                        Source = fighter,
                        State = RoundActionState.Spell,
                        Spell = spell,
                        Targets = targets
                    };
                }
            }

            // do attack
            return new RoundAction
            {
                Source = fighter,
                State = RoundActionState.Fight,
                Targets = new[]
                {
                   target
                }
            };
        }

        private void OrderActions()
        {
            this._roundActions.Sort((x, y) => x.Source.Agility - y.Source.Agility);
        }

        private void DoActions()
        {
            var action = this._roundActions.FirstOrDefault(item =>
                CanBeAttacked(item.Source) && (item.Targets == null || item.Targets.Any(CanBeAttacked)));
            if (action == null)
            {
                this._state = EncounterRoundState.EndRound;
                return;
            }

            this._roundActions.Remove(action);
            this._state = EncounterRoundState.DoingActions;
            var message = action.Source.UpdateStatusEffects(this._round, DurationType.Rounds, this._game);
            var endFight = false;
            void DoneAction()
            {
                if (endFight)
                {
                    this._state = EncounterRoundState.EndEncounter;
                    this._game.ResumeGame();
                }
                else
                {
                    this._state = EncounterRoundState.StartDoingActions;
                }    
            }
            
            if (action.Source.IsDead)
            {
                new FightTalkWindow(this._ui, "").Show(message,
                    DoneAction);
                return;
            }
            
            switch (action.State)
            {
                case RoundActionState.Run:
                    string m;
                    (m, endFight) = this.Run(action.Source);
                    message += m;
                    break;
                case RoundActionState.Fight:
                    message += this.Fight(action.Source, action.Targets);
                    break;
                case RoundActionState.Spell:
                    message += action.Spell.Cast(action.Targets, action.Source, this._game, this._round);
                    break;
                case RoundActionState.Item:
                {
                    var target = action.Targets.FirstOrDefault();
                    if (target != null)
                    {
                        if (target != action.Source)
                        {
                            message += $"{action.Source.Name} Uses {action.Item.Name} on {target.Name}";
                            UseItem(target, action.Item, this._game.Party);
                        }
                        else
                        {
                            message += $"{action.Source.Name} Uses {action.Item.Name}";
                            UseItem(target, action.Item, this._game.Party);
                        }
                    }

                    break;
                }
                case RoundActionState.Nothing:
                    message += $"{action.Source.Name} doesn't do anything";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (action.Targets != null)
            {
                foreach (var monster in action.Targets.OfType<MonsterInstance>().Where(item => item.IsDead))
                {
                    monster.Image.SetVisible(false);
                }
            }

            new FightTalkWindow(this._ui, "").Show(message,
                DoneAction);
        }

        private string Fight(IFighter source, IEnumerable<IFighter> targets)
        {
            var message = "";
            this._game.Sounds.PlaySoundEffect("prepare-attack", true);
            var totalDamage = 0;
            foreach (var target in targets)
            {
                message += $"{source.Name} Attacks {target.Name}.\n";
                int damage;
                if (Random.NextInt(Math.Max(30 - source.Agility / 2, 2)) == 0)
                {
                    damage = Random.NextInt(source.Attack + 20 * source.Level) + 10;
                    message += "Heroic maneuver!\n";
                }
                else
                {
                    var defence = (100 - Math.Min(target.Defence, 99)) / 100f;
                    var attack = Random.NextInt(source.Attack);
                    damage = attack != 0 ? Math.Max((int) ( attack * defence), 1) : 0;
                }
                
                if (damage <= 0)
                {
                    damage = 0;
                }
                    
                totalDamage += damage;
                target.Health -= damage;
                if (damage == 0)
                {
                    message += $"{target.Name} was unharmed\n";
                }
                else
                {
                    target.PlayDamageAnimation();
                    message += $"{target.Name} took {damage} points of damage\n";
                }

                if (target.Health <= 0)
                {
                    message += "and has died!\n";
                    target.Health = 0;
                }
            }

            this._game.Sounds.PlaySoundEffect(totalDamage == 0 ? "miss" : "receive-damage");
            return message;
        }

        private (string message, bool endFight) Run(IFighter fighter)
        {
            var message = $"{fighter.Name} Tried to run\n";
            if (Random.NextInt(5) == 1)
            {
                return (message, false);
            }

            var endFight = false;
            this._game.Sounds.PlaySoundEffect("stairs-up");
            message += "And got away\n";
            switch (fighter)
            {
                case Hero _:
                    this._game.Sounds.PlayMusic(EndFightSong);
                    endFight = true;
                    break;
                case MonsterInstance monster:
                    monster.RanAway = true;
                    monster.Image.SetVisible(false);
                    break;
            }

            return (message, endFight);
        }

        private static void UseItem(IFighter hero, ItemInstance item, Party party)
        {
            if (hero == null)
            {
                return;
            }
            
            switch (item.Type)
            {
                case ItemType.OneUse:
                    hero.Use(item);
                    party.Items.Remove(item);
                    break;
                case ItemType.Armor:
                case ItemType.Weapon:
                    item.UnEquip(party.Members);
                    hero.Equip(item);
                    break;
                case ItemType.Key:
                    break;
                case ItemType.Gold:
                    break;
                case ItemType.Unknown:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private void EndEncounter()
        {
            var talkWindow = new FightTalkWindow(this._ui, "");
            if (this._game.Party.Members.Count(CanBeAttacked) == 0)
            {
                talkWindow.Show("Everyone has died!", this._game.ShowMainMenu);
            }
            else
            {
                this._game.Sounds.StopMusic();
                this._game.Sounds.PlaySoundEffect("victory", true);
                this._game.Sounds.PlayMusic(EndFightSong);
                var xp = this._monsters.Where(monster=> monster.IsDead).Sum(monster => (int)monster.Xp) / this._game.Party.Members.Count(member => !member.IsDead);
                if (xp == 0)
                {
                    xp = 1;
                }

                var gold = this._monsters.Where(monster=> monster.IsDead).Sum(monster => monster.Gold);
                this._game.Party.Gold += gold;

                var monsterName = this._monsters.Count == 1 ?$"the {this._monsters.First().Name}"  : "all the enemies";
                var levelUpMessage =$"You have defeated {monsterName},\nEach party member has gained {xp}XP\nand the party got {gold} gold\n";
                var leveledUp = false;
                foreach (var member in this._game.Party.Members.Where(member => !member.IsDead))
                {
                    member.Xp += (ulong)xp;
                        while (member.CheckLevelUp(this._game.ClassLevelStats,this._game.Spells, out var message))
                        {
                            leveledUp = true;
                            levelUpMessage += message;
                        }
                }

                if (leveledUp)
                {
                    this._game.Sounds.PlaySoundEffect("level-up");
                }

                // clear round only status effects
                foreach (var hero in this._heroes)
                {
                    foreach (var effect in hero.Status.Where(i => i.DurationType == DurationType.Rounds))
                    {
                        hero.RemoveEffect(effect);
                    }
                }
                    
                talkWindow.Show(levelUpMessage, this._game.ResumeGame);
            }
        }
        
    }
}
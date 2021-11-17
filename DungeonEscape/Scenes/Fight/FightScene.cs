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
            Item
        }
        
        private class RoundAction
        {
            public IFighter Source { get; set; }
            public RoundActionState State { get; set; }
            public Spell Spell { get; set; }
            public ItemInstance Item { get; set; }
            public IEnumerable<IFighter> Target { get; set; }
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
        private readonly List<MonsterInstance> _monsters = new List<MonsterInstance>();
        private UiSystem _ui;
        private EncounterRoundState _state = EncounterRoundState.Begin;
        private readonly List<RoundAction> _roundActions = new List<RoundAction>();
        private List<Hero> _heroes;

        public FightScene(IGame game, IEnumerable<Monster> monsters)
        {
            this._game = game;
            foreach (var monster in monsters)
            {
                this._monsters.Add(new MonsterInstance(monster));
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            this.ClearColor = Color.Black;
            this.SetDesignResolution(MapScene.ScreenWidth, MapScene.ScreenHeight,
                MapScene.SceneResolution);

            this.AddRenderer(new DefaultRenderer());
            this._ui = new UiSystem(this.CreateEntity("ui-canvas").AddComponent(new UICanvas()));
            this._ui.Canvas.SetRenderLayer(999);
            this._ui.Canvas.Stage.GamepadActionButton = null;

            var table = this._ui.Canvas.Stage.AddElement(new Table());
            table.SetFillParent(true);
            table.Center();
            foreach (var monster in this._monsters)
            {
                monster.Image = table.Add(new Image(monster.Info.Image)).Pad(10).GetElement<Image>();
            }
            
            var partyWindow = new PartyStatusWindow(this._game.Party,this._ui.Canvas);
            partyWindow.ShowWindow();

            var monsterName = this._monsters.Count == 1 ?$"a {this._monsters.First().Name}"  : $"{this._monsters.Count} enemies";
            var message =$"You have encountered {monsterName}!";
            
            new FightTalkWindow(this._ui, "Start Fight").Show(message, ()=> this._state = EncounterRoundState.StartRound);
        }

        public override void Update()
        {
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

        private void ChooseAction(Hero hero, Action<RoundAction> done)
        {
            var selectAction =
                new SelectWindow<string>(this._ui, "Select Action", new Point(10, MapScene.ScreenHeight / 3 * 2));
            
            var options = new List<string> {"Fight"};
            var availableSpells = hero.GetSpells(this._game.Spells).Where(item => item.IsEncounterSpell && item.Cost <= hero.Magic).ToList();
            if (availableSpells.Count != 0)
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
                                Target = new []{monster}
                            };

                            done(newAction);
                        });
                        return;
                    }
                    case "Spell":
                    {
                        var selectItem = new SpellWindow(this._ui, new Point(10, MapScene.ScreenHeight / 3 * 2));
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

                                            newAction.Target = new[] {monster};
                                            done(newAction);
                                        });
                                        return;
                                    }

                                    newAction.Target = this._monsters.Where(item => !item.IsDead);
                                    done(newAction);
                                    return;

                                }

                                if (spell.Targets == Target.Single)
                                {
                                    if (this._game.Party.Members.Count(member => !member.IsDead) == 1)
                                    {
                                        newAction.Target = new[] {hero};
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

                                        newAction.Target = new[] {target};
                                        done(newAction);
                                    });
                                    return;
                                }

                                newAction.Target = this._game.Party.Members.Where(item => !item.IsDead);
                                done(newAction);
                            });
                        return;
                    }
                    case "Item":
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
                                    Target = new []{hero},
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
                                    Target = new []{target},
                                    Item = item
                                };

                                done(newAction);
                            });
                        });
                        return;
                    }
                    case "Run":
                    {
                        var newAction = new RoundAction
                        {
                            Source = hero,
                            State = RoundActionState.Run
                        };
                        done(newAction); 
                        return;
                    }
                }

            });
        }

        private RoundAction ChooseAction(MonsterInstance monster)
        {
            var hasAttackSpell = monster.Info.Spells.Count(item => item.IsAttackSpell && item.Cost <= monster.Magic) !=
                                 0;
            var hasHeal =
                monster.Info.Spells.Count(item => item.Type == SpellType.Heal && item.Cost <= monster.Magic) != 0;
            if (hasHeal && (float) monster.Health / monster.MaxHealth * 100f < 10)
            {
                var healSpells =
                    monster.Info.Spells.Where(item => item.Type == SpellType.Heal && item.Cost <= monster.Magic)
                        .ToArray();

                var spell = healSpells[Random.NextInt(healSpells.Length)];
                var spellAction = new RoundAction
                {
                    Source = monster,
                    State = RoundActionState.Spell,
                    Spell = spell
                };
                return spellAction;
            }

            if (hasAttackSpell && Random.NextInt(6) != 0)
            {
                var attackSpells =
                    monster.Info.Spells.Where(item => item.IsAttackSpell && item.Cost <= monster.Magic).ToArray();

                var spell = attackSpells[Random.NextInt(attackSpells.Length)];

                var targets = spell.Targets == Target.Group
                    ? this._game.Party.Members.Where(CanBeAttacked).OfType<IFighter>()
                    : new List<IFighter>
                    {
                        this._game.Party.Members.Where(CanBeAttacked).ToArray()[
                            Random.NextInt(this._game.Party.Members.Count)]
                    };
                    
                var spellAction = new RoundAction
                {
                    Source = monster,
                    State = RoundActionState.Spell,
                    Spell = spell,
                    Target = targets
                };
                return spellAction;
            }

            var fightAction = new RoundAction
            {
                Source = monster,
                State = RoundActionState.Fight,
                Target = new[]
                {
                    this._game.Party.Members.Where(CanBeAttacked).ToArray()[
                        Random.NextInt(this._game.Party.Members.Count)]
                }
            };
            
            return fightAction;
        }

        private void OrderActions()
        {
            this._roundActions.Sort((x, y) => x.Source.Agility - y.Source.Agility);
        }

        private void DoActions()
        {
            var action = this._roundActions.FirstOrDefault(item =>
                CanBeAttacked(item.Source) && (item.Target == null || item.Target.Any(CanBeAttacked)));
            if (action == null)
            {
                this._state = EncounterRoundState.EndRound;
            }
            else
            {
                this._roundActions.Remove(action);
                this._state = EncounterRoundState.DoingActions;
                var message = "";
                switch (action.State)
                {
                    case RoundActionState.Run:
                        message = $"{action.Source.Name} Tried to run\n";
                        if (Random.NextInt(5) != 1)
                        {
                            message += "And got away";
                            switch (action.Source)
                            {
                                case Hero _:
                                    this._state = EncounterRoundState.EndEncounter;
                                    new FightTalkWindow(this._ui, "Fight").Show(message, this._game.ResumeGame);
                                    return;
                                case MonsterInstance monster:
                                    monster.RanAway = true;
                                    monster.Image.SetVisible(false);
                                    break;
                            }
                        }

                        break;
                    case RoundActionState.Fight:
                        foreach (var target in action.Target)
                        {
                            message = $"{action.Source.Name} Attacks {target.Name}.\n";
                            int damage;
                            if (Random.NextInt(22 - action.Source.Agility / 2) == 0)
                            {
                                damage = Random.NextInt(action.Source.Attack + 20 * action.Source.Level) + 10;
                                message += "Heroic maneuver!\n";
                            }
                            else
                            {
                                damage = Random.NextInt(action.Source.Attack);
                            }

                            damage -= (int) (damage * target.Defence / 100f);
                            target.Health -= damage;

                            if (damage == 0)
                            {
                                message += $"{target.Name} was unharmed\n";
                            }
                            else
                            {
                                message += $"{target.Name} took {damage} points of damage\n";
                            }

                            if (target.Health > 0)
                            {
                                continue;
                            }

                            message += "and has died!\n";
                            target.Health = 0;
                        }

                        break;
                    case RoundActionState.Spell:
                        message = action.Spell.Cast(action.Target, action.Source, this._game);
                        break;
                    case RoundActionState.Item:
                    {
                        var target = action.Target.FirstOrDefault();
                        if (target != null)
                        {
                            if (target != action.Source)
                            {
                                message = $"{action.Source.Name} Uses {action.Item.Name} on {target.Name}";
                                UseItem(target as Hero, action.Item, this._game.Party);
                            }
                            else
                            {
                                message = $"{action.Source.Name} Uses {action.Item.Name}";
                                UseItem(target as Hero, action.Item, this._game.Party);
                            }
                        }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (action.Target != null)
                {
                    foreach (var target in action.Target)
                    {
                        if (target is MonsterInstance monster && monster.IsDead)
                        {
                            monster.Image.SetVisible(false);
                        }
                    }

                }

                new FightTalkWindow(this._ui, "Fight").Show(message,
                    () => { this._state = EncounterRoundState.StartDoingActions; });
            }
        }

        private static void UseItem(Hero hero, ItemInstance item, Party party)
        {
            if (hero == null)
            {
                return;
            }
            
            switch (item.Type)
            {
                case ItemType.OneUse:
                    item.Use(hero);
                    party.Items.Remove(item);
                    break;
                case ItemType.Armor:
                case ItemType.Weapon:
                case ItemType.Shield:
                    item.Equip(hero, party.Items, party.Members);
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
            var talkWindow = new FightTalkWindow(this._ui, "End Fight");
            if (this._game.Party.Members.Count(CanBeAttacked) == 0)
            {
                talkWindow.Show("Everyone has died!", this._game.ShowMainMenu);
            }
            else
            {
                var xp = this._monsters.Where(monster=> monster.IsDead).Sum(monster => monster.Info.Xp) / this._game.Party.Members.Count(member => !member.IsDead);
                if (xp == 0)
                {
                    xp = 1;
                }

                var gold = this._monsters.Where(monster=> monster.IsDead).Sum(monster => monster.Info.Gold);
                this._game.Party.Gold += gold;

                var monsterName = this._monsters.Count == 1 ?$"the {this._monsters.First().Name}"  : "all the enemies";
                var levelUpMessage =$"You have defeated {monsterName},\nEach party member has gained {xp}XP\nand the party got {gold} gold\n";
                foreach (var member in this._game.Party.Members.Where(member => !member.IsDead))
                {
                    member.Xp += xp;
                        while (member.CheckLevelUp(this._game.ClassLevelStats,this._game.Spells, out var message))
                        {
                            levelUpMessage += message;
                        }
                }
                
                talkWindow.Show(levelUpMessage, this._game.ResumeGame);
            }
        }
        
    }
}
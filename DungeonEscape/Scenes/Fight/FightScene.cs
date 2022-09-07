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
            Skill
        }
        
        private class RoundAction
        {
            public IFighter Source { get; init; }
            public RoundActionState State { get; init; }
            public Spell Spell { get; init; }
            public ItemInstance Item { get; init; }
            public List<IFighter> Targets { get; set; }
            public Skill Skill { get; set; }
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
        private readonly List<MonsterInstance> _monsters = new();
        private IEnumerable<MonsterInstance> AliveMonsters => this._monsters.Where(item => !item.IsDead && !item.RanAway);
        private UiSystem _ui;
        private EncounterRoundState _state = EncounterRoundState.Begin;
        private readonly List<RoundAction> _roundActions = new();
        private List<Hero> _heroes;
        private int _round;

        public FightScene(IGame game, IEnumerable<Monster> monsters, Biome biome)
        {
            this._game = game;
            this._biome = biome;
            foreach (var monsterGroup in monsters.OrderBy(i => i.MinLevel).GroupBy(i => i.Id))
            {
                var monsterId = 'A';
                foreach (var monster  in monsterGroup)
                {
                    var instance = new MonsterInstance(monster, game);
                    if (monsterGroup.Count() != 1)
                    {
                        instance.Name = $"{instance.Name} {monsterId}";
                        monsterId++;
                    }
                    
                    this._monsters.Add(instance);
                }
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
            
            this._game.Sounds.PlayMusic(new [] {Songs[Random.NextInt(Songs.Count)]});
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
            this._heroes = this._game.Party.AliveMembers.ToList();
            foreach (var monster in this.AliveMonsters)
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
            if (_game.Party.Members.Any(CanBeAttacked) &&
                AliveMonsters.Any())
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
            
            var skills = hero.GetSkills(this._game.Skills).ToList();
            options.AddRange(skills.Select(skill => skill.Name));

            var availableItems = hero.Items.Where(item => item.Type is ItemType.OneUse or ItemType.RepeatableUse).ToList();
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
                        this.ChooseRun(hero, done);
                        return;
                    }
                    default:
                    {
                        ChooseSkill(hero, selection, skills, done);
                        return;
                    }
                }

            });
        }
        
        List<IFighter> GetTargets(Target targetType, int maxTargets, List<IFighter> availableTargets)
        {
            if (targetType != Target.Group)
            {
                return new List<IFighter>
                {
                    availableTargets.ToArray()[Random.NextInt(availableTargets.Count)]
                };
            }

            if (maxTargets == 0)
            {
                return availableTargets.ToList();
            }

            var targets = new List<IFighter>();
            for (var i = 0; i < maxTargets; i++)
            {
                targets.Add(availableTargets.ToArray()[Random.NextInt(availableTargets.Count)]);
            }

            return targets;
        }

        // ReSharper disable once UnusedParameter.Local
        private void ChooseSkill(IFighter hero, string selection, List<Skill> skills, Action<RoundAction> done)
        {
            var skill = skills.FirstOrDefault(i => i.Name == selection);
            if (skill == null)
            {
                done(null);
                return;
            }

            var newAction = new RoundAction
            {
                Source = hero,
                State = RoundActionState.Skill,
                Skill = skill
            };

            if (skill.Targets != Target.Single)
            {
                newAction.Targets = GetTargets(skill.Targets, skill.MaxTargets, this.AliveMonsters.Cast<IFighter>().ToList());
                done(newAction);
                return;
            }

            if (this.AliveMonsters.Count() == 1)
            {
                var monster = this.AliveMonsters.First();
                newAction.Targets = new List<IFighter> { monster };
                done(newAction);
                return;
            }

            var selectTarget = new SelectWindow<MonsterInstance>(this._ui, "SelectMonster",
                new Point(10, MapScene.ScreenHeight / 3 * 2), 250);
            selectTarget.Show(this.AliveMonsters, monster =>
            {
                if (monster == null)
                {
                    done(null);
                    return;
                }

                newAction.Targets = new List<IFighter> { monster };
                done(newAction);
            });
        }

        private  void ChooseRun(IFighter hero, Action<RoundAction> done)
        {
            done(new RoundAction
            {
                Source = hero,
                State = RoundActionState.Run,
                Targets = this.AliveMonsters.OfType<IFighter>().ToList()
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

                if (this._game.Party.AliveMembers.Count() == 1)
                {
                    var newAction = new RoundAction
                    {
                        Source = hero,
                        State = RoundActionState.Item,
                        Targets = new List<IFighter> { hero },
                        Item = item
                    };

                    done(newAction);
                    return;
                }

                var selectTarget = new SelectHeroWindow(this._ui,
                    new Point(10, MapScene.ScreenHeight / 3 * 2));
                selectTarget.Show(this._game.Party.AliveMembers, target =>
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
                        Targets = new List<IFighter> { target },
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
                    if (spell.Targets != Target.Single)
                    {
                        newAction.Targets = GetTargets(spell.Targets, spell.MaxTargets, this.AliveMonsters.Cast<IFighter>().ToList());
                        done(newAction);
                        return;
                    }

                    if (this.AliveMonsters.Count() == 1)
                    {
                        var monster = this.AliveMonsters.First();
                        newAction.Targets = new List<IFighter> { monster };
                        done(newAction);
                        return;
                    }
                        
                    var selectTarget = new SelectWindow<MonsterInstance>(this._ui, "SelectMonster",
                        new Point(10, MapScene.ScreenHeight / 3 * 2), 250);
                    selectTarget.Show(this.AliveMonsters, monster =>
                    {
                        if (monster == null)
                        {
                            done(null);
                            return;
                        }

                        newAction.Targets = new List<IFighter> { monster };
                        done(newAction);
                    });
                    return;

                }

                if (spell.Targets != Target.Single)
                {
                    newAction.Targets = GetTargets( spell.Targets, spell.MaxTargets,this._game.Party.AliveMembers.Cast<IFighter>().ToList());
                    done(newAction);
                    return;
                }

                {
                    if (this._game.Party.AliveMembers.Count() == 1)
                    {
                        newAction.Targets = new List<IFighter> { hero };
                        done(newAction);
                        return;
                    }

                    var selectTarget = new SelectHeroWindow(this._ui,
                        new Point(10, MapScene.ScreenHeight / 3 * 2));
                    selectTarget.Show(this._game.Party.AliveMembers, target =>
                    {
                        if (target == null)
                        {
                            done(null);
                            return;
                        }

                        newAction.Targets = new List<IFighter> { target };
                        done(newAction);
                    });
                }
            });
        }

        private void ChooseFight(IFighter hero, Action<RoundAction> done)
        {
            if (this.AliveMonsters.Count() == 1)
            {
                var monster = this.AliveMonsters.First();
                var newAction = new RoundAction
                {
                    Source = hero,
                    State = RoundActionState.Fight,
                    Targets = new List<IFighter> { monster }
                };

                done(newAction);
                return;
            }

            var selectTarget = new SelectWindow<MonsterInstance>(this._ui, "SelectMonster",
                new Point(10, MapScene.ScreenHeight / 3 * 2), 250);
            selectTarget.Show(this.AliveMonsters, monster =>
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
                    Targets = new List<IFighter> { monster }
                };

                done(newAction);
            });
        }

        private RoundAction ChooseAction(IFighter fighter)
        {
            if (fighter.Status.Any(i => i.Type == EffectType.Sleep))
            {
                return new RoundAction
                {
                    Source = fighter,
                    State = RoundActionState.Nothing
                };
            }

            var availableTargets = this._game.Party.Members.OfType<IFighter>().Where(CanBeAttacked).ToList();
            if (fighter.Status.Any(i => i.Type == EffectType.Confusion))
            {
                availableTargets.AddRange(this.AliveMonsters);
            }

            if (fighter.Status.Any(i => i.Type == EffectType.StopSpell))
            {
                // check to cast heal spell
                if (fighter.GetSpells(_game.Spells).Any(item => item.Type == SkillType.Heal && item.Cost <= fighter.Magic)
                     && (float) fighter.Health / fighter.MaxHealth * 100f < 10)
                {
                    var healSpells =
                        fighter.GetSpells(this._game.Spells).Where(item => item.Type == SkillType.Heal && item.Cost <= fighter.Magic)
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
                if (fighter.GetSpells(_game.Spells).Any(item => item.IsAttackSpell && item.Cost <= fighter.Magic)) // has spells
                {
                    var attackSpells =
                        fighter.GetSpells(this._game.Spells).Where(item => item.IsAttackSpell && item.Cost <= fighter.Magic).ToArray();

                    var spell = attackSpells[Random.NextInt(attackSpells.Length)];
                    return new RoundAction
                    {
                        Source = fighter,
                        State = RoundActionState.Spell,
                        Spell = spell,
                        Targets =  GetTargets(spell.Targets, spell.MaxTargets, availableTargets)
                    };
                }
            }

            var skills = fighter.GetSkills(_game.Skills).ToArray();
            if (skills.Any() && Dice.RollD100() > 75) // has skills
            {
                var skill = skills[Random.NextInt(skills.Length)];
                if (skill.Type == SkillType.Flee)
                {
                    return new RoundAction
                    {
                        Source = fighter,
                        State = RoundActionState.Run,
                        Targets = this._game.Party.Members.Where(CanBeAttacked).OfType<IFighter>().ToList()
                    };
                }
                
                return new RoundAction
                {
                    Source = fighter,
                    State = RoundActionState.Skill,
                    Skill = skill,
                    Targets = GetTargets(skill.Targets, skill.MaxTargets, availableTargets)
                };
            }

            // do attack
            return new RoundAction
            {
                Source = fighter,
                State = RoundActionState.Fight,
                Targets = new List<IFighter> { availableTargets.ToArray()[Random.NextInt(availableTargets.Count)] }
            };
        }

        private void DoActions()
        {
            var action = this._roundActions.OrderByDescending(i => i.Source.Agility).FirstOrDefault(item =>
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
                    (m, endFight) = this.Run(action.Source, action.Targets);
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
                            message += UseItem(action.Source, target, action.Item);
                        }
                        else
                        {
                            message += $"{action.Source.Name} Uses {action.Item.Name}";
                            message += UseItem(action.Source, target, action.Item);
                        }
                    }

                    break;
                }
                case RoundActionState.Nothing:
                    message += $"{action.Source.Name} doesn't do anything";
                    break;
                case RoundActionState.Skill:
                    message += this.DoSkill(action.Skill, action.Targets, action.Source);
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

        private string DoSkill(Skill skill, List<IFighter> targets, IFighter source)
        {
            var message = "";
            if (skill.IsAttackSkill)
            {
                this._game.Sounds.PlaySoundEffect("prepare-attack", true);
            }
            
            var hit = false;
            foreach (var target in targets)
            {
                if (skill.DoAttack || skill.Type == SkillType.Attack)
                {
                    message += Fight(source, target, out var damage);
                    if (damage != 0)
                    {
                        hit = true;
                    }
                }

                if (target.IsDead) continue;
                if (skill.IsAttackSkill)
                {
                    message += skill.Type == SkillType.Attack && !skill.DoAttack
                        ? $"{source.Name} attacks {target.Name} with {skill.EffectName}.\n"
                        : "";
                    
                    if (!source.CanHit(target))
                    {
                        message += !skill.DoAttack ? $"{target.Name} dodges the {skill.EffectName}" : "";
                        continue;
                    }
                }

                var result = skill.Do(target, source, this._game, this._round);
                if (skill.IsAttackSkill)
                {
                    if (result.Item2)
                    {
                        continue;
                    }
                    hit = true;
                }

                message += result.Item1;
            }

            if (skill.IsAttackSkill || skill.DoAttack)
            {
                this._game.Sounds.PlaySoundEffect(hit ? "receive-damage" : "miss");
            }

            return message;
        }

        private static string Fight(IFighter source, IFighter target, out int damage)
        {
            var message = $"{source.Name} Attacks {target.Name}.\n";
            damage = 0;
            if (source.CanCriticalHit(target))  // check for critical hit
            {
                var attack = Random.NextInt(source.CriticalAttack);
                damage = target.CalculateDamage(attack);
                message += "Heroic maneuver!\n";
                message += $"{target.Name}";
            }
            else if (source.CanHit(target)) // Can the source hit the target?
            {
                var attack = Random.NextInt(source.Attack);
                damage = target.CalculateDamage(attack);
                message += $"{target.Name}";
            }
            else
            {
                damage = 0;
                message += $"{target.Name} dodges the attack and";
            }

            if (damage <= 0)
            {
                damage = 0;
            }
            
            if (damage == 0)
            {
                message += $" was unharmed\n";
            }
            else
            {
                target.Health -= damage;
                target.PlayDamageAnimation();
                message += $" took {damage} points of damage\n";
                message += target.HitCheck();
            }

            if (target.IsDead)
            {
                message += "and has died!\n";
                target.Health = 0;
            }

            return message;
        }

        private string Fight(IFighter source, IEnumerable<IFighter> targets)
        {
            var message = "";
            this._game.Sounds.PlaySoundEffect("prepare-attack", true);
            var totalDamage = 0;
            foreach (var target in targets)
            {
                message = Fight(source, target, out var damage);
                totalDamage += damage;
            }

            this._game.Sounds.PlaySoundEffect(totalDamage == 0 ? "miss" : "receive-damage");
            return message;
        }

        private (string message, bool endFight) Run(IFighter fighter, IEnumerable<IFighter> targets)
        {
            var message = $"{fighter.Name} Tried to run\n";
            if (!fighter.CanHit(targets.OrderByDescending(item => item.Agility).First()))
            {
                return (message, false);
            }

            var endFight = false;
            this._game.Sounds.PlaySoundEffect("stairs-up");
            message += "And got away\n";
            switch (fighter)
            {
                case Hero:
                    this._game.Sounds.PlayMusic(new []{EndFightSong});
                    endFight = true;
                    break;
                case MonsterInstance monster:
                    monster.RanAway = true;
                    monster.Image.SetVisible(false);
                    break;
            }

            return (message, endFight);
        }

        private string UseItem(IFighter source, IFighter target, ItemInstance item)
        {
            if (target == null)
            {
                return "";
            }

            var result = "";
            bool worked;
            switch (item.Type)
            {
                case ItemType.OneUse:
                    (result, worked) = item.Use(source, target, this._game, this._round);
                    if (!worked)
                    {
                        source.Items.Remove(item);
                    }
                    break;
                case ItemType.RepeatableUse:
                    (result, _) = item.Use(source, target, this._game, this._round);
                    if (!item.HasCharges)
                    {
                        source.Items.Remove(item);
                        result += " and has been destroyed.";
                    }
                    break;
                case ItemType.Armor:
                case ItemType.Weapon:
                case ItemType.Key:
                case ItemType.Gold:
                case ItemType.Unknown:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;
        }
        
        private void EndEncounter()
        {
            var talkWindow = new FightTalkWindow(this._ui, "");
            if (!_game.Party.Members.Any(CanBeAttacked))
            {
                talkWindow.Show("Everyone has died!", this._game.ShowMainMenu);
            }
            else
            {
                this._game.Sounds.StopMusic();
                this._game.Sounds.PlaySoundEffect("victory", true);
                this._game.Sounds.PlayMusic(new []{EndFightSong});
                var xp = this._monsters.Where(monster=> monster.IsDead).Sum(monster => (int)monster.Xp) / this._game.Party.AliveMembers.Count();
                if (xp == 0)
                {
                    xp = 1;
                }

                var gold = this._monsters.Where(monster=> monster.IsDead).Sum(monster => monster.Gold);
                this._game.Party.Gold += gold;

                var foundItems = new List<Item>();
                foreach (var monster in _monsters)
                {
                    foundItems.AddRange(monster.Items.Select(item => item.Item));
                }

                var monsterName = this._monsters.Count(i => i.IsDead) == 1 ?$"the {this._monsters.First(i => i.IsDead).Name}"  : "all the enemies";
                var endFightMessage =$"You have defeated {monsterName},\nEach party member has gained {xp}XP\nand the party got {gold} gold\n";
                if (Dice.RollD20() > 18)
                {
                    foundItems.Add(_game.CreateChestItem(this._game.Party.MaxLevel()));
                }

                if (foundItems.Any())
                {
                    var foundItemMessage = "";
                    var questMessage = "";
                    foreach (var foundItem in foundItems)
                    {
                        if (foundItem.Type == ItemType.Gold)
                        {
                            foundItemMessage += $"You found {foundItem.Cost} Gold\n";
                            this._game.Party.Gold += foundItem.Cost;
                        }
                        else
                        {
                            var member = this._game.Party.AddItem(new ItemInstance(foundItem));
                            if (member != null)
                            {
                                foundItemMessage += $"{member.Name} found a {foundItem.Name}\n";
                                questMessage += this._game.CheckQuest(foundItem, false);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(foundItemMessage))
                    {
                        this._game.Sounds.PlaySoundEffect("treasure");
                        endFightMessage +=  $"You found a chest and opened it!\n"+foundItemMessage+questMessage;
                    }
                }
                
                var leveledUp = false;
                foreach (var member in this._game.Party.AliveMembers)
                {
                    member.Xp += (ulong)xp;
                        while (member.CheckLevelUp(this._game.ClassLevelStats,this._game.Spells, out var message))
                        {
                            leveledUp = true;
                            endFightMessage += message;
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
                    
                talkWindow.Show(endFightMessage, this._game.ResumeGame);
            }
        }
        
    }
}
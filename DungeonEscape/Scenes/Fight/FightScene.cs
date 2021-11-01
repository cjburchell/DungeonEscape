namespace DungeonEscape.Scenes.Fight
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;
    using State;
    using Random = Nez.Random;

    public class FightScene : Nez.Scene
    {

        private enum RoundActionState
        {
            Run,
            Fight,
            Spell,
            Item,
        }
        
        private class RoundAction
        {
            public Fighter Source { get; set; }
            public RoundActionState State { get; set; }
            public Spell Spell { get; set; }
            public Item Item { get; set; }
            public Fighter Target { get; set; }
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
        
        private readonly IGame game;
        private readonly List<MonsterInstance> monsters = new List<MonsterInstance>();
        private UISystem ui;
        private EncounterRoundState state = EncounterRoundState.Begin;
        private List<RoundAction> roundActions = new List<RoundAction>();
        private List<Hero> heros;

        public FightScene(IGame game, IEnumerable<Monster> monsters)
        {
            this.game = game;
            foreach (var monster in monsters)
            {
                this.monsters.Add(new MonsterInstance(monster));
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            this.ClearColor = Color.Black;
            this.SetDesignResolution(MapScene.ScreenWidth, MapScene.ScreenHeight,
                MapScene.SceneResolution);

            this.AddRenderer(new DefaultRenderer());
            this.ui = new UISystem(this.CreateEntity("ui-canvas").AddComponent(new UICanvas()));
            this.ui.Canvas.SetRenderLayer(999);
            this.ui.Canvas.Stage.GamepadActionButton = null;

            var table = this.ui.Canvas.Stage.AddElement(new Table());
            table.SetFillParent(true);
            table.Center();
            foreach (var monster in this.monsters)
            {
                monster.Image = table.Add(new Image(monster.Info.Image)).Pad(10).GetElement<Image>();
            }
            
            var partyWindow = new PartyStatusWindow(this.game.Party,this.ui.Canvas);
            partyWindow.ShowWindow();

            var monsterName = this.monsters.Count == 1 ?$"a {this.monsters.First().Name}"  : $"{this.monsters.Count} enemies";
            var message =$"You have encountered {monsterName}!";
            
            var talkWindow = new TalkWindow(this.ui, "Start Fight");
            talkWindow.Show(message, ()=> this.state = EncounterRoundState.StartRound);
        }

        public override void Update()
        {
            base.Update();
            
            // Each Party member chooises there action
            // Each Monster chooses there action
            // Choose who goes first
            // in the order of actions do the combat
            // end the encouter if eather oll monsters are dead or party members
            
            switch (this.state)
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
            this.roundActions.Clear();
            this.heros = this.game.Party.Members.ToList();
            foreach (var monster in this.monsters)
            {
                var action = this.ChooseAction(monster);
                this.roundActions.Add(action);
            }
            this.state = EncounterRoundState.ChooseAction;
        }

        private void ChoosingActions()
        {
            var nextHero = this.heros.FirstOrDefault(member => !member.IsDead);
            if (nextHero == null)
            {
                this.OrderActions();
                this.state = EncounterRoundState.StartDoingActions;
            }
            else
            {
                this.state = EncounterRoundState.ChoosingAction;
                this.ChooseAction(nextHero, action =>
                {
                    this.state = EncounterRoundState.ChooseAction;
                    if (action == null)
                    {
                        return;
                    }

                    this.heros.Remove(nextHero);
                    this.roundActions.Add(action);
                });
            }
        }

        private void EndRound()
        {
            if (this.game.Party.Members.Count(member => !member.IsDead) != 0 &&
                this.monsters.Count(monster => !monster.IsDead) != 0)
            {
                this.state = EncounterRoundState.StartRound;
                return;
            }

            this.state = EncounterRoundState.EndEncounter;
            this.EndEncounter();
        }
        
        private void ChooseAction(Hero hero, Action<RoundAction> done)
        {
            var newAction = new RoundAction
            {
                Source = hero,
                State = RoundActionState.Fight,
                Target = this.monsters.Where(member => !member.IsDead).ToArray()[Random.NextInt(this.monsters.Count)]
            };
            // TODO: add selection of action
            done(newAction);
        }
        
        private RoundAction ChooseAction(MonsterInstance monster)
        {
            var newAction = new RoundAction
            {
                Source = monster,
                State = RoundActionState.Fight,
                Target = this.game.Party.Members.Where(member => !member.IsDead).ToArray()[Random.NextInt(this.game.Party.Members.Count)]
            };
            // TODO: Randomize targets and actions
            return newAction;
        }

        private void OrderActions()
        {
            this.roundActions.Sort((x, y) => x.Source.Agility - y.Source.Agility);
        }

        private void DoActions()
        {
            var action = this.roundActions.FirstOrDefault(item=> !item.Source.IsDead && (item.Target == null || !item.Target.IsDead));
            if (action == null)
            {
                this.state = EncounterRoundState.EndRound;
            }
            else
            {
                this.roundActions.Remove(action);
                this.state = EncounterRoundState.DoingActions;
                var message = "";
                switch (action.State)
                {
                    case RoundActionState.Run:
                        message = $"{action.Source.Name} Tries to run";
                        // TODO: Run away
                        break;
                    case RoundActionState.Fight:
                        message = $"{action.Source.Name} Attacks {action.Target.Name}.\n";
                        var damage = 0;
                        if(Random.NextInt(22-(action.Source.Agility/2))==0)
                        {
                            damage = Nez.Random.NextInt(action.Source.Attack+20*action.Source.Level)+10;
                            message += "Heroic maneuver!\n";
                        }
                        else
                        {
                            damage = Nez.Random.NextInt(action.Source.Attack);
                        }

                        damage -= (int)(damage * action.Target.Defence / 100f);
                        action.Target.Health -= damage;
                        
                        if (damage == 0)
                        {
                            message += $"{action.Target.Name} was unharmed\n";
                        }
                        else
                        {
                            message += $"{action.Target.Name} took {damage} points of damage\n";
                        }

                        if (action.Target.Health <= 0)
                        {
                            action.Target.Health = 0;
                            message += $"and has died!\n";
                            if (action.Target is MonsterInstance monster)
                            {
                                monster.Image.SetVisible(false);
                            }
                        }
                        
                        break;
                    case RoundActionState.Spell:
                        if (action.Target != null)
                        {
                            message = $"{action.Source.Name} Casts {action.Spell.Name} on {action.Target.Name}";
                        }
                        else
                        {
                            message = $"{action.Source.Name} Casts {action.Spell.Name}";
                        }
                        // TODO: Cast Spell
                        break;
                    case RoundActionState.Item:
                        if (action.Target != null)
                        {
                            message = $"{action.Source.Name} Uses {action.Item.Name} on {action.Target.Name}";
                        }
                        else
                        {
                            message = $"{action.Source.Name} Item {action.Item.Name}";
                        }
                        // TODO: Use Item
                        break;
                }
                
                new TalkWindow(this.ui, "Fight").Show(message, ()=>
                {
                    this.state = EncounterRoundState.StartDoingActions;
                });
            }
        }
        
        private void EndEncounter()
        {
            if (this.game.Party.Members.Count(member => !member.IsDead) == 0)
            {
                var talkWindow = new TalkWindow(this.ui, "End Fight");
                talkWindow.Show("Everyone has died!", this.game.ShowMainMenu);
            }
            else
            {
                var xp = monsters.Sum(monster => monster.Info.XP) / this.game.Party.Members.Count(member => !member.IsDead);
                if (xp == 0)
                {
                    xp = 1;
                }

                var gold = monsters.Sum(monster => monster.Info.Gold);
                this.game.Party.Gold += gold;

                var monsterName = this.monsters.Count == 1 ?$"the {this.monsters.First().Name}"  : "all the enemies";
                var levelUpMessage =$"You have defeated {monsterName}, Each party member has gained {xp}XP\nand got {gold} gold!";
                foreach (var member in this.game.Party.Members.Where(member => !member.IsDead))
                {
                    member.XP += xp;
                        while (member.CheckLevelUp(this.game.Spells, out var message))
                        {
                            levelUpMessage += message;
                        }
                }

                var talkWindow = new TalkWindow(this.ui, "End Fight");
                talkWindow.Show(levelUpMessage, this.game.ResumeGame);
            }
        }
        
    }
}
namespace DungeonEscape.Scenes.Fight
{
    using System.Collections.Generic;
    using System.Linq;
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez;
    using Nez.UI;
    using State;

    public class FightScene : Nez.Scene
    {
        private readonly IGame game;
        private readonly List<Monster> monsters;
        private UISystem ui;

        public FightScene(IGame game, List<Monster> monsters)
        {
            this.game = game;
            this.monsters = monsters;
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
                table.Add(new Image(monster.Image)).Pad(10);
            }
            
            var partyWindow = new PartyStatusWindow(this.game.Party,this.ui.Canvas);
            partyWindow.ShowWindow();
            
            this.EndEncounter();
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
                var xp = monsters.Sum(monster => monster.XP) / this.game.Party.Members.Count(member => !member.IsDead);
                if (xp == 0)
                {
                    xp = 1;
                }

                var gold = monsters.Sum(monster => monster.Gold);
                this.game.Party.Gold += gold;

                var monsterName = this.monsters.Count == 1 ? this.monsters.First().Name : "enemies";
                var levelUpMessage =$"You have defeated the {monsterName}, Each party member has gained {xp}XP\nand got {gold} gold!";
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
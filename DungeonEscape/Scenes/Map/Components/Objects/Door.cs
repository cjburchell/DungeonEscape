using System;
using System.Linq;

namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using Common.Components.UI;
    using Nez.Tiled;
    using State;

    public class Door: SolidObject
    {
        private readonly UiSystem _ui;

        private bool IsOpen => this.ObjectState.IsOpen != null && this.ObjectState.IsOpen.Value;

        public Door(TmxObject tmxObject, ObjectState state, TmxMap map, UiSystem ui, IGame gameState) : base(tmxObject, state, map, gameState)
        {
            this.ObjectState.IsOpen ??= this.TmxObject.Properties.ContainsKey("IsOpen") &&
                                        bool.Parse(this.TmxObject.Properties["IsOpen"]);
            this._ui = ui;
            this.ObjectState.Level = tmxObject.Properties.ContainsKey("DoorLevel") ? int.Parse(tmxObject.Properties["DoorLevel"]) : 0;
        }

        public override void Initialize()
        {
            base.Initialize();
            this.DisplayVisual(!this.IsOpen);
            this.SetEnableCollider(!this.IsOpen);
        }

        public override bool CanDoAction()
        {
            if (this.IsOpen)
            {
                return false;
            }
            
            foreach (var member in this.GameState.Party.AliveMembers)
            {
                var key = member.Items.FirstOrDefault(item => item.Item.IsKey && item.MinLevel == this.ObjectState.Level);
                if (key != null)
                {
                    return true;
                }
            }

            return false;
        }

        public override void OnAction(Action done)
        {
            if (this.CanDoAction())
            {
                done();
                return;
            }

            var result = this.GameState.Party.OpenDoor(this.ObjectState, this.GameState);
            this.SetEnableCollider(!this.IsOpen);
            this.DisplayVisual(!this.IsOpen);
            this.Collideable = !this.IsOpen;
            if (string.IsNullOrEmpty(result))
            {
                return;
            }
            
            this.GameState.IsPaused = true;
            new TalkWindow(this._ui).Show(result, done);
        }

        public override void Update()
        {
            this.SetEnableCollider(!this.IsOpen);
            this.DisplayVisual(!this.IsOpen);
            this.Collideable = !this.IsOpen;
            base.Update();
        }
    }
}
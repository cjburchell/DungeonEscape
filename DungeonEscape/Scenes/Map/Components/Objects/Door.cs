using Nez;

namespace Redpoint.DungeonEscape.Scenes.Map.Components.Objects
{
    using Common.Components.UI;
    using Nez.Tiled;
    using State;

    public class Door: SolidObject
    {
        private readonly UiSystem _ui;
        private readonly int _level;

        private bool IsOpen
        {
            get => this.State.IsOpen != null && this.State.IsOpen.Value;
            set => this.State.IsOpen = value;
        }

        public Door(TmxObject tmxObject, ObjectState state, TmxMap map, UiSystem ui, IGame gameState) : base(tmxObject, state, map, gameState)
        {
            this.State.IsOpen ??= this.TmxObject.Properties.ContainsKey("IsOpen") &&
                                  bool.Parse(this.TmxObject.Properties["IsOpen"]);
            this._ui = ui;
            this._level = tmxObject.Properties.ContainsKey("DoorLevel") ? int.Parse(tmxObject.Properties["DoorLevel"]) : 0;
        }

        public override void Initialize()
        {
            base.Initialize();
            this.DisplayVisual(!this.IsOpen);
            this.SetEnableCollider(!this.IsOpen);
        }

        public override bool OnAction(Party party)
        {
            if (this.IsOpen)
            {
                return false;
            }

            if (!party.CanOpenDoor(this._level))
            {

                this.GameState.IsPaused = true;
                new TalkWindow(this._ui).Show("Unable to open door", () =>
                {
                    this.GameState.IsPaused = false;
                });
                return true;
            }
            this.GameState.Sounds.PlaySoundEffect("door");
            this.IsOpen = true;
            this.SetEnableCollider(false);
            this.DisplayVisual(false);
            this.Collideable = false;
            return true;
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
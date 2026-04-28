using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class DungeonEscapeGameState : MonoBehaviour
    {
        [SerializeField]
        private string defaultMapId = "overworld";

        [SerializeField]
        private int defaultColumn = 30;

        [SerializeField]
        private int defaultRow = 25;

        public GameSave CurrentSave { get; private set; }

        public Party Party
        {
            get { return CurrentSave == null ? null : CurrentSave.Party; }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (CurrentSave != null)
            {
                return;
            }

            CurrentSave = CreateDefaultSave();
        }

        public void SetCurrentMap(string mapId)
        {
            EnsureInitialized();
            Party.CurrentMapId = TiledMapLoader.NormalizeMapId(mapId);
            Party.CurrentMapIsOverWorld = Party.CurrentMapId == "overworld";
        }

        public void SetCurrentPosition(WorldPosition position)
        {
            EnsureInitialized();
            Party.CurrentPosition = position;

            if (Party.CurrentMapIsOverWorld)
            {
                Party.OverWorldPosition = position;
            }
        }

        public void IncrementStepCount()
        {
            EnsureInitialized();
            Party.StepCount++;
        }

        private GameSave CreateDefaultSave()
        {
            var party = new Party
            {
                PlayerName = "Player",
                CurrentMapId = TiledMapLoader.NormalizeMapId(defaultMapId),
                CurrentPosition = new WorldPosition(defaultColumn, defaultRow)
            };
            party.CurrentMapIsOverWorld = party.CurrentMapId == "overworld";
            party.OverWorldPosition = party.CurrentPosition.Value;

            return new GameSave
            {
                Party = party,
                IsQuick = true
            };
        }

        public static DungeonEscapeGameState GetOrCreate()
        {
            var state = FindObjectOfType<DungeonEscapeGameState>();
            if (state != null)
            {
                state.EnsureInitialized();
                return state;
            }

            state = new GameObject("DungeonEscapeGameState").AddComponent<DungeonEscapeGameState>();
            state.EnsureInitialized();
            return state;
        }
    }
}

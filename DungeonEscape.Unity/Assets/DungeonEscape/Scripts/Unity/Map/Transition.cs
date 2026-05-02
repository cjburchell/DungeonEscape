using Redpoint.DungeonEscape.Unity.Core;
using Redpoint.DungeonEscape.Unity.UI;
using Redpoint.DungeonEscape.Unity.Map;
using Redpoint.DungeonEscape.Unity.Rendering;
using Redpoint.DungeonEscape.Unity.Map.Tiled;
namespace Redpoint.DungeonEscape.Unity.Map
{
    public sealed class Transition
    {
        public string MapId { get; set; }
        public string SpawnId { get; set; }
        public bool UseSavedOverWorldPosition { get; set; }
    }
}

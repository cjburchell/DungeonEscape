using Redpoint.DungeonEscape.Data;
using System.Collections.Generic;

namespace Redpoint.DungeonEscape.State
{
    public interface IGame
    {
        Party Party { get; }
        List<ClassStats> ClassLevelStats { get; }
        ISounds Sounds { get; }

        void SetMap(string mapId = null, string spawnId = null, WorldPosition? point = null);

        Item CreateChestItem(int level, Rarity? rarity = null);
        Item CreateGold(int gold);
        Item GetCustomItem(string itemId);
    }

    public interface ISounds
    {
        void PlaySoundEffect(string name, bool stopCurrent = false);
    }
}

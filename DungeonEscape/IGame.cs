using Nez.Tiled;

namespace DungeonEscape
{
    public interface IGame
    {
        TmxMap GetMap(int mapId);

        int CurrentMapId { get; set; }

        State.Player Player { get; }
        
        bool IsPaused { get; set; }
    }
}
using System.Collections;
using System.Collections.Generic;

namespace Redpoint.DungeonEscape.Scenes.Map.Components
{
    public interface IPlayer
    {
        List<ICollidable> CurrentlyOverObjects { get; }
    }
}
namespace Redpoint.DungeonEscape.State
{
    public interface Fighter
    {
        string Name { get; }
        int Health { get; set; }
        int Magic { get; set; }
        int Attack { get; }
        int Defence { get; }
        int Agility { get; }
        bool IsDead { get; }
        int Level { get; }
        int MaxHealth { get; }
        bool RanAway { get; }
    }
}
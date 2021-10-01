using System;

namespace DungeonEscape
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using var game = new DungeonEscapeGame();
            game.Run();
        }
    }
}
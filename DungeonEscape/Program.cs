namespace Redpoint.DungeonEscape
{
    using System;

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using var game = new Game();
            game.Run();
        }
    }
}
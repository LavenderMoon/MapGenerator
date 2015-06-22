#if WINDOWS || LINUX
using System;

namespace MapGenerator
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        private static MapGenApp _game;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (_game = new MapGenApp())
            {
                _game.Run();
            }
        }
    }
}
#endif

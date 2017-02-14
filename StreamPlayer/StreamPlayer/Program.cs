using System;

namespace StreamPlayer
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Player game = new Player())
            {
                game.Run();
            }
        }
    }
#endif
}


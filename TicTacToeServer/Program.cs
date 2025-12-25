using System;

namespace TicTacToeServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Tic Tac Toe Server ===");
            Console.WriteLine("Starting server...\n");

            GameServer server = new GameServer();
            server.Start();

            Console.WriteLine("\nPress any key to stop the server...");
            Console.ReadKey();

            server.Stop();
            Console.WriteLine("Server stopped.");
        }
    }
}

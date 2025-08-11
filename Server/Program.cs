using System;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server("127.0.0.1", 8888);
            server.Start();

            Console.WriteLine("Нажмите любую клавишу для остановки сервера.");
            Console.ReadKey();
            
            server.Stop();
        }
    }
}
// Program.cs
using CafeTime.Server.Network;
using CafeTime.Server.Services;
using Org.BouncyCastle.Tls;

namespace CafeTime.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Cafe Time Server";
            Console.WriteLine("Starting Cafe Time Server...");

            // Initialize database service
            DatabaseService.Initialize();

            // Start TCP server
            TcpServer server = new TcpServer();
            server.Start(5000);
        }
    }
}
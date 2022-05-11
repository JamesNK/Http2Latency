using System.Net;
using Grpc.Core;
using GrpcTestService;

namespace GrpcCoreTestService
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Server server = new Server
            {
                Services = { TestProxy.TestProxy.BindService(new TestProxyService()) },
                Ports = { new ServerPort("localhost", 81, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("Server listening on port " + 81);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}

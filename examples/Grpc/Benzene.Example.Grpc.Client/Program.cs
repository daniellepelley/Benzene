using System.Diagnostics;
using Grpc.Net.Client;

namespace Benzene.Example.Grpc.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            using var channel = GrpcChannel.ForAddress("https://localhost:7268");
            var client = new Greeter.GreeterClient(channel);
            do
            {
                Console.ReadKey();

                var stopWatch = new Stopwatch();
                stopWatch.Start();
                try
                {
                    var reply = await client.SayHelloAsync(
                        new HelloRequest { Name = "GreeterClient" });
                    stopWatch.Stop();
                    Console.WriteLine("Greeting: " + reply.Message + " in " + stopWatch.ElapsedMilliseconds + "ms");
                }
                catch(Exception ex)
                {
                    Console.Error.Write(ex.Message);
                }
            } while (true);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}

using System.Diagnostics;
using Grpc.Core;
using Grpc.Net.Client;

namespace Benzene.Example.Grpc.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Press any key to call the Greeter service (unary, server-stream, client-stream, bidi); Ctrl+C to exit.");
            using var channel = GrpcChannel.ForAddress("https://localhost:7268");
            var client = new Greeter.GreeterClient(channel);

            do
            {
                Console.ReadKey(true);

                // A fresh x-correlation-id per call - a Benzene correlation-id middleware on the
                // server side would pick this up from GrpcMessageHeadersGetter the same way it
                // would from an HTTP header.
                var headers = new Metadata { { "x-correlation-id", Guid.NewGuid().ToString() } };
                var stopWatch = Stopwatch.StartNew();

                try
                {
                    await CallSayHello(client, headers);
                    await CallSayHelloServerStream(client, headers);
                    await CallSayHelloClientStream(client, headers);
                    await CallSayHelloBidiStream(client, headers);
                }
                catch (RpcException ex)
                {
                    Console.Error.WriteLine($"RPC failed: {ex.Status}");
                }

                stopWatch.Stop();
                Console.WriteLine($"Done in {stopWatch.ElapsedMilliseconds}ms" + Environment.NewLine);
            } while (true);
        }

        private static async Task CallSayHello(Greeter.GreeterClient client, Metadata headers)
        {
            var call = client.SayHelloAsync(new HelloRequest { Name = "GreeterClient" }, headers);
            var reply = await call;
            Console.WriteLine($"SayHello: {reply.Message} [benzene-status: {BenzeneStatus(call.GetTrailers())}]");
        }

        private static async Task CallSayHelloServerStream(Greeter.GreeterClient client, Metadata headers)
        {
            using var call = client.SayHelloServerStream(new HelloRequest { Name = "GreeterClient" }, headers);
            while (await call.ResponseStream.MoveNext(CancellationToken.None))
            {
                Console.WriteLine($"SayHelloServerStream: {call.ResponseStream.Current.Message}");
            }
            Console.WriteLine($"SayHelloServerStream: [benzene-status: {BenzeneStatus(call.GetTrailers())}]");
        }

        private static async Task CallSayHelloClientStream(Greeter.GreeterClient client, Metadata headers)
        {
            using var call = client.SayHelloClientStream(headers);
            foreach (var name in new[] { "Alice", "Bob", "Carol" })
            {
                await call.RequestStream.WriteAsync(new HelloRequest { Name = name });
            }
            await call.RequestStream.CompleteAsync();

            var reply = await call.ResponseAsync;
            Console.WriteLine($"SayHelloClientStream: {reply.Message} [benzene-status: {BenzeneStatus(call.GetTrailers())}]");
        }

        private static async Task CallSayHelloBidiStream(Greeter.GreeterClient client, Metadata headers)
        {
            using var call = client.SayHelloBidiStream(headers);
            var readTask = Task.Run(async () =>
            {
                while (await call.ResponseStream.MoveNext(CancellationToken.None))
                {
                    Console.WriteLine($"SayHelloBidiStream: {call.ResponseStream.Current.Message}");
                }
            });

            foreach (var name in new[] { "Dave", "Erin" })
            {
                await call.RequestStream.WriteAsync(new HelloRequest { Name = name });
            }
            await call.RequestStream.CompleteAsync();
            await readTask;

            Console.WriteLine($"SayHelloBidiStream: [benzene-status: {BenzeneStatus(call.GetTrailers())}]");
        }

        private static string BenzeneStatus(Metadata trailers)
        {
            return trailers.FirstOrDefault(e => !e.IsBinary && e.Key == "benzene-status")?.Value ?? "(none)";
        }
    }
}

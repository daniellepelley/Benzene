// using System.Text;
// using Xunit;
// using zipkin4net.Tracers.Zipkin;
// using zipkin4net;
// using zipkin4net.Transport.Http;
// using ILogger = zipkin4net.ILogger;

// namespace Benzene.Integration.Test;
//
// [Collection("Sequential")]
// public class SqsMessageSenderBuilderTest //: IClassFixture<ZipkinFixture>
// {
//     [Fact]
//     public async Task Zipkin()
//     {
//         var logger = new ZipkinLogger();
//
//         TraceManager.SamplingRate = 1.0f;
//         // Obtain the Zipkin endpoint in the Tracing Analysis console. Note that the endpoint does not contain /api/v2/spans.
//         var sender = new HttpZipkinSender("http://localhost:9411", "application/json"); // It should implement IZipkinSender
//
//         sender.Send(Encoding.UTF8.GetBytes("{}"));
//
//         var tracer = new ZipkinTracer(sender, new JSONSpanSerializer());
//         TraceManager.RegisterTracer(tracer);
//
//         TraceManager.Start(logger);
//
//         //Run your application
//         var trace = Trace.Create();
//         trace.Record(Annotations.ServerRecv());
//         trace.Record(Annotations.ServiceName("benzene"));
//         trace.Record(Annotations.Rpc("GET"));
//         var child = trace.Child();
//         child.Record(Annotations.ServerRecv());
//         child.Record(Annotations.Tag("fdf", "dsdsd"));
//         child.Record(Annotations.ServerSend());
//
//
//         trace.Record(Annotations.ServerSend());
//         trace.Record(Annotations.Tag("http.url", "<url>")); //adds binary annotation
//
//         trace.ForceSampled();
//
//
//         Trace.Current = trace;
//
//         //On shutdown
//         TraceManager.Stop();
//
//
//
//         await Task.Delay(2000);
//     }
//
//
// }
//
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.XRay.Recorder.Core;
using Benzene.Aws.Core;
using Benzene.Diagnostics.Timers;
using Newtonsoft.Json;

namespace Benzene.Examples.Aws.Tests.Helpers;

public class TestLambdaHosting
{
    private readonly ILambdaEntryPoint _lambdaEntryPoint;

    public TestLambdaHosting(ILambdaEntryPoint lambdaEntryPoint)
    {
        _lambdaEntryPoint = lambdaEntryPoint;
    }
    
    private static Stream StringToStream(string src)
    {
        var byteArray = Encoding.UTF8.GetBytes(src);
        return new MemoryStream(byteArray);
    }

    private static Stream ObjectToStream(object obj)
    {
        return StringToStream(JsonConvert.SerializeObject(obj));
    }

    private static T StreamToObject<T>(Stream stream)
    {
        var json = StreamToString(stream);
        return JsonConvert.DeserializeObject<T>(json);
    }

    private static string StreamToString(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public async Task<Stream> SendEventAsync(object awsEvent, ILambdaContext lambdaContext)
    {
        AWSXRayRecorder.Instance.BeginSegment("Test");
        using var debugTimer = new DebugProcessTimer("test_send_event");
        var response = await _lambdaEntryPoint.FunctionHandlerAsync(ObjectToStream(awsEvent), lambdaContext);
        AWSXRayRecorder.Instance.EndSegment();
        return response;
    }

    public async Task<Stream> SendEventAsync(object awsEvent)
    {
        var lambdaContext = new TestLambdaContext();
        var testLogger = new ThreadSafeTestLambdaLogger();
        lambdaContext.Logger = testLogger;
        return await SendEventAsync(awsEvent, lambdaContext);
    }

    public async Task<TResponse> SendEventAsync<TResponse>(object awsEvent)
    {
        var stream = await SendEventAsync(awsEvent);
        return StreamToObject<TResponse>(stream);
    }
}
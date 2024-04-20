using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.XRay.Recorder.Core;
using Benzene.Abstractions;
using Benzene.Aws.Core;
using Newtonsoft.Json;

namespace Benzene.Tools.Aws;

public sealed class AwsLambdaBenzeneTestHost : IBenzeneTestHost, IDisposable
{
    private readonly IAwsLambdaEntryPoint _awsLambdaEntryPoint;

    private static Stream StringToStream(string src)
    {
        var byteArray = Encoding.UTF8.GetBytes(src);
        return new MemoryStream(byteArray);
    }

    private static Stream ObjectToStream(object obj)
    {
        return StringToStream(JsonConvert.SerializeObject(obj));
    }

    public static T StreamToObject<T>(Stream stream)
    {
        var json = StreamToString(stream);
        return JsonConvert.DeserializeObject<T>(json);
    }

    public static string StreamToString(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public AwsLambdaBenzeneTestHost(IAwsLambdaEntryPoint awsLambdaEntryPoint)
    {
        _awsLambdaEntryPoint = awsLambdaEntryPoint;
    }

    public async Task<Stream> SendEventAsync(object awsEvent, ILambdaContext? lambdaContext)
    {
        lambdaContext ??= new TestLambdaContext();
        AWSXRayRecorder.Instance.BeginSegment("Test");
        var response = await _awsLambdaEntryPoint.FunctionHandler(ObjectToStream(awsEvent), lambdaContext);
        AWSXRayRecorder.Instance.EndSegment();
        return response;
    }

    public Task<Stream> SendEventAsync(object awsEvent)
    {
        var lambdaContext = new TestLambdaContext();
        var testLogger = new ThreadSafeTestLambdaLogger();
        lambdaContext.Logger = testLogger;
        return SendEventAsync(awsEvent, lambdaContext);
    }

    public async Task<TResponse> SendEventAsync<TResponse>(object awsEvent, ILambdaContext? lambdaContext = null)
    {
        var stream = await SendEventAsync(awsEvent, lambdaContext);
        return StreamToObject<TResponse>(stream);
    }

    public void Dispose()
    {
        _awsLambdaEntryPoint.Dispose();
    }

    public Task<TResponse> SendEventAsync<TResponse>(object awsEvent)
    {
        return SendEventAsync<TResponse>(awsEvent, null);
    }
}

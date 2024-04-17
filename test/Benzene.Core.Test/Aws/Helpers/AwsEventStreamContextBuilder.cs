using System.IO;
using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.TestUtilities;
using Benzene.Aws.Core.AwsEventStream;
using Newtonsoft.Json;

namespace Benzene.Test.Aws.Helpers;

public static class AwsEventStreamContextBuilder 
{
    private static Stream StringToStream(string src)
    {
        var byteArray = Encoding.UTF8.GetBytes(src);
        return new MemoryStream(byteArray);
    }

    public static Stream ObjectToStream(object obj)
    {
        var jsonSerializer = new DefaultLambdaJsonSerializer();
        var output = new MemoryStream();
        jsonSerializer.Serialize(obj, output);
        return output;
    }

    public static T StreamToObject<T>(Stream stream)
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

    public static AwsEventStreamContext Build(object awsEvent, ILambdaContext lambdaContext)
    {
        return new AwsEventStreamContext(ObjectToStream(awsEvent), lambdaContext);
    }

    public static AwsEventStreamContext Build(object awsEvent)
    {
        var lambdaContext = new TestLambdaContext();
        return Build(awsEvent, lambdaContext);
    }

    public static AwsEventStreamContext AwsEventStreamContext(this object awsEvent, ILambdaContext lambdaContext)
    {
        return new AwsEventStreamContext(ObjectToStream(awsEvent), lambdaContext);
    }

    public static AwsEventStreamContext AwsEventStreamContext(this object awsEvent)
    {
        return new AwsEventStreamContext(ObjectToStream(awsEvent), new TestLambdaContext());
    }
}

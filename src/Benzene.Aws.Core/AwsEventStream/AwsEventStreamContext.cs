using System.IO;
using Amazon.Lambda.Core;

namespace Benzene.Aws.Core.AwsEventStream;

public class AwsEventStreamContext
{
    public AwsEventStreamContext(Stream stream, ILambdaContext lambdaContext)
    {
        Stream = stream;
        Response = new MemoryStream();
        LambdaContext = lambdaContext;
    }

    public Stream Stream { get; }
    public ILambdaContext LambdaContext { get; }
    public Stream Response { get; set; }
}
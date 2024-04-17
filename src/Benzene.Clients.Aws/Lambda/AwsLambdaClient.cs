using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Benzene.Abstractions.Serialization;

namespace Benzene.Clients.Aws.Lambda
{
    public class AwsLambdaClient : IAwsLambdaClient
    {
        private readonly IAmazonLambda _amazonLambda;
        private readonly ISerializer _serializer;

        public AwsLambdaClient(IAmazonLambda amazonLambda)
        {
            _amazonLambda = amazonLambda;
            _serializer = new JsonSerializer();
        }

        public async Task<TResponse> SendMessageAsync<TRequest, TResponse>(TRequest request, string functionName, InvocationType invocationType)
        {
            var invokeRequest = new InvokeRequest
            {
                FunctionName = functionName,
                InvocationType = invocationType,
                Payload = _serializer.Serialize(request)
            };

            var lambdaResponse = await _amazonLambda.InvokeAsync(invokeRequest);
            return InvocationType.Event == invocationType
                    ? default
                    : StreamToObject<TResponse>(lambdaResponse.Payload);
        }

        private T StreamToObject<T>(Stream stream)
        {
            var json = StreamToString(stream);
            return _serializer.Deserialize<T>(json);
        }

        private static string StreamToString(Stream stream)
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}

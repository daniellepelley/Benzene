using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Benzene.Abstractions.Serialization;

namespace Benzene.Clients.Aws.Lambda
{
    /// <summary>
    /// An <see cref="IAwsLambdaClient"/> implementation that invokes functions via the AWS Lambda SDK,
    /// serializing the request and deserializing the response payload as JSON.
    /// </summary>
    public class AwsLambdaClient : IAwsLambdaClient
    {
        private readonly IAmazonLambda _amazonLambda;
        private readonly ISerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="AwsLambdaClient"/> class.
        /// </summary>
        /// <param name="amazonLambda">The Lambda client used to invoke functions.</param>
        public AwsLambdaClient(IAmazonLambda amazonLambda)
        {
            _amazonLambda = amazonLambda;
            _serializer = new JsonSerializer();
        }

        /// <summary>
        /// Invokes the given function with the given request.
        /// </summary>
        /// <typeparam name="TRequest">The request payload type.</typeparam>
        /// <typeparam name="TResponse">
        /// The expected response payload type. When <paramref name="invocationType"/> is
        /// <see cref="InvocationType.Event"/>, no response is returned and <c>default</c> is returned
        /// instead.
        /// </typeparam>
        /// <param name="request">The request to serialize as the invocation payload.</param>
        /// <param name="functionName">The name of the function to invoke.</param>
        /// <param name="invocationType">Whether to invoke fire-and-forget or request/response.</param>
        /// <returns>A task that resolves to the deserialized response.</returns>
        public async Task<TResponse> SendMessageAsync<TRequest, TResponse>(TRequest request, string functionName, InvocationType invocationType)
        {
            var invokeRequest = new InvokeRequest
            {
                FunctionName = functionName,
                InvocationType = invocationType,
                Payload = _serializer.Serialize(request)
            };

            var lambdaResponse = await _amazonLambda.InvokeAsync(invokeRequest);

            if (InvocationType.Event == invocationType)
            {
                return default;
            }

            // A RequestResponse invoke where the function threw returns HTTP 200 with FunctionError set
            // and an error object (not the normal output) as the payload. Surface that as a failure
            // rather than mis-deserializing the error object into TResponse.
            if (!string.IsNullOrEmpty(lambdaResponse.FunctionError))
            {
                throw new AwsLambdaFunctionErrorException(functionName, lambdaResponse.FunctionError,
                    StreamToString(lambdaResponse.Payload));
            }

            return StreamToObject<TResponse>(lambdaResponse.Payload);
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

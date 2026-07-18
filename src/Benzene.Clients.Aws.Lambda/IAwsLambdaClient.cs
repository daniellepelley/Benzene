using System.Threading.Tasks;
using Amazon.Lambda;

namespace Benzene.Clients.Aws.Lambda
{
    /// <summary>
    /// Represents a client for invoking AWS Lambda functions.
    /// </summary>
    public interface IAwsLambdaClient
    {
        /// <summary>
        /// Invokes the given function with the given request.
        /// </summary>
        /// <typeparam name="TRequest">The request payload type.</typeparam>
        /// <typeparam name="TResponse">
        /// The expected response payload type. Ignored when <paramref name="invocationType"/> is
        /// <see cref="InvocationType.Event"/>, since no response is returned.
        /// </typeparam>
        /// <param name="request">The request to serialize as the invocation payload.</param>
        /// <param name="functionName">The name of the function to invoke.</param>
        /// <param name="invocationType">Whether to invoke fire-and-forget or request/response.</param>
        /// <returns>A task that resolves to the deserialized response.</returns>
        Task<TResponse> SendMessageAsync<TRequest, TResponse>(TRequest request, string functionName, InvocationType invocationType);
    }
}

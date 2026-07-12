using System;
using System.Threading.Tasks;
using Amazon.Lambda;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients.Common;
using Benzene.Results;

namespace Benzene.Clients.Aws.Lambda
{
    /// <summary>
    /// A Benzene message client that invokes a target AWS Lambda function, choosing between fire-and-forget
    /// (<see cref="InvocationType.Event"/>) and request/response (<see cref="InvocationType.RequestResponse"/>)
    /// invocation based on whether <c>TResponse</c> is <see cref="Void"/>.
    /// </summary>
    public class AwsLambdaBenzeneMessageClient : IBenzeneMessageClient
    {
        private readonly string _lambdaName;
        private readonly IBenzeneLogger _logger;
        private readonly AwsLambdaClient _awsLambdaClient;
        private readonly ISerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="AwsLambdaBenzeneMessageClient"/> class.
        /// </summary>
        /// <param name="lambdaName">The name of the target Lambda function.</param>
        /// <param name="amazonLambda">The Lambda client used to invoke the function.</param>
        /// <param name="logger">The logger used to record invocation outcomes and failures.</param>
        public AwsLambdaBenzeneMessageClient(string lambdaName, IAmazonLambda amazonLambda, IBenzeneLogger logger)
        {
            _awsLambdaClient = new AwsLambdaClient(amazonLambda);
            _lambdaName = lambdaName;
            _logger = logger;
            _serializer = new JsonSerializer();
        }

        /// <summary>
        /// Sends the request as a message to the target Lambda function.
        /// </summary>
        /// <typeparam name="TRequest">The request payload type.</typeparam>
        /// <typeparam name="TResponse">
        /// The expected response payload type. When this is <see cref="Void"/>, the function is invoked
        /// fire-and-forget (<see cref="InvocationType.Event"/>); otherwise it is invoked and awaited for a
        /// response (<see cref="InvocationType.RequestResponse"/>).
        /// </typeparam>
        /// <param name="request">The client request to send.</param>
        /// <returns>
        /// A task that resolves to an accepted result for fire-and-forget invocations, the mapped result of
        /// the Lambda's response for request/response invocations, or a service-unavailable result if the
        /// invocation threw.
        /// </returns>
        public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
        {
            try
            {
                var lambdaRequest = new BenzeneMessageClientRequest(request.Topic, request.Headers, _serializer.Serialize(request.Message));

                var invocationType = typeof(TResponse).Name == "Void"
                    ? InvocationType.Event
                    : InvocationType.RequestResponse;

                if (invocationType == InvocationType.Event)
                {
                    _logger.LogInformation("Fire and Forget message {receiverTopic} sent to {receiver}", request.Topic, _lambdaName);
                    await _awsLambdaClient.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(lambdaRequest, _lambdaName, invocationType);
                    return BenzeneResult.Accepted<TResponse>();
                }

                var response = await _awsLambdaClient.SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(lambdaRequest, _lambdaName, invocationType);
                var clientResult = response.AsBenzeneResult<TResponse>(_serializer);

                _logger.LogInformation("Request and Response message {receiverTopic} sent to {receiver} with status {receiverStatus}",
                    request.Topic, _lambdaName, clientResult.Status);

                return clientResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sending message {receiverTopic} to {receiver} failed", request.Topic, _lambdaName);
                return BenzeneResult.ServiceUnavailable<TResponse>(ex.Message);
            }
        }

        /// <summary>
        /// Disposes the client. No-op; the client holds no disposable resources of its own.
        /// </summary>
        public void Dispose()
        {
            // Method intentionally left empty.
        }

    }
}

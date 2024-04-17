using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.Results;

namespace Benzene.Aws.ApiGateway
{
    public class ApiGatewayContext : IHasMessageResult
    {
        public ApiGatewayContext(APIGatewayProxyRequest apiGatewayProxyRequest)
        {
            ApiGatewayProxyRequest = apiGatewayProxyRequest;
            MessageResult = Benzene.Core.Results.MessageResult.Empty();
        }

        public APIGatewayProxyRequest ApiGatewayProxyRequest { get; }
        public APIGatewayProxyResponse ApiGatewayProxyResponse { get; set; }
        public IMessageResult MessageResult { get; set; }
    }
}

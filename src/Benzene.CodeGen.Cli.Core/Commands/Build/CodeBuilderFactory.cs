using Benzene.CodeGen.ApiGateway;
using Benzene.CodeGen.Client;
using Benzene.CodeGen.Core;
using Benzene.CodeGen.Markdown;
using Benzene.Schema.OpenApi.EventService;

namespace Benzene.CodeGen.Cli.Core.Commands.Build;

public class CodeBuilderFactory
{
    public ICodeBuilder<EventServiceDocument> Create(ICommandPayload codePayload)
    {
        var serviceName = LambdaNameParser.GetServiceName(codePayload.LambdaName);
        switch (codePayload.Output)
        {
            case "readme":
                return new LambdaServiceMarkdownBuilder(codePayload.LambdaName, serviceName, "");
            case "api-gateway":
                return new ApiGatewayBuilderV1(codePayload.LambdaName);
            case "message-handlers":
                return new MessageHandlerBuilder(LambdaNameParser.GetNamespace(codePayload.LambdaName, "Service"));
            default:
                return new MessageClientSdkBuilder(serviceName, LambdaNameParser.GetNamespace(codePayload.LambdaName, "Client"));
        }
    }
}

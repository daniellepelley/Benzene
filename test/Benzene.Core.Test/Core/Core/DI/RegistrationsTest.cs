using System;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Microsoft.Dependencies;
using Xunit;

namespace Benzene.Test.Core.Core.DI;

public class RegistrationsTest
{
    public RegistrationsTest()
    {
        //To load this assembly
        new ApiGatewayContext(new APIGatewayProxyRequest());
    }

    [Fact]
    public void RegistrationTest()
    {
        var result = RegistrationErrorHandler.CheckType(typeof(IDefaultStatuses));
    
        Assert.Contains("Benzene.Core", result);
        Assert.Contains("IDefaultStatuses", result);
        Assert.Contains(".UsingBenzene(x => x.AddBenzene())", result);
    }

    [Fact]
    public void RegistrationTest_IMiddlewareFactory()
    {
        var result = RegistrationErrorHandler.CheckType(typeof(IMiddlewareFactory));

        Assert.Contains("Benzene.Core", result);
        Assert.Contains("IMiddlewareFactory", result);
        Assert.Contains(".UsingBenzene(x => x.AddBenzene())", result);
    }


    [Fact]
    public void RegistrationTest_Generics()
    {
        var result = RegistrationErrorHandler.CheckType(typeof(MessageRouter<BenzeneMessageContext>));

        // Assert.Contains("Benzene.Core", benzeneResult);
        Assert.Contains("MessageRouter<>", result);
        // Assert.Contains(".UsingBenzene(x => x.AddMessageHandlers(<assemblies>))", benzeneResult);

        Assert.Contains("Benzene.Aws.Lambda.Core", result);
        Assert.Contains(".UsingBenzene(x => x.AddMessageHandlers(<assemblies>))", result);
    }
    
    [Fact]
    public void RegistrationTest_Exception_UnableToResolve()
    {
        var result = RegistrationErrorHandler.CheckException(new Exception("Unable to resolve service for type 'Benzene.Abstractions.MessageHandlers.Mappers.IMessageTopicGetter`1[Benzene.Aws.Lambda.ApiGateway.ApiGatewayContext]' while attempting to activate 'Benzene.Core.Mappers.MessageGetter`1[Benzene.Aws.Lambda.ApiGateway.ApiGatewayContext]'."));

        Assert.Contains("Benzene.Aws.Lambda.ApiGateway", result);
        Assert.Contains("Benzene.Abstractions.MessageHandlers.Mappers.IMessageTopicGetter<Benzene.Aws.Lambda.ApiGateway.ApiGatewayContext>", result);
        Assert.Contains(".UsingBenzene(x => x.AddApiGateway())", result);
    }

    [Fact]
    public void RegistrationTest_Exception_NoService()
    {
        var result = RegistrationErrorHandler.CheckException(new Exception("No service for type 'Benzene.Abstractions.Middleware.IMiddlewareFactory' has been registered."));

        Assert.Contains("Benzene.Core", result);
        Assert.Contains("IMiddlewareFactory", result);
        Assert.Contains(".UsingBenzene(x => x.AddBenzene())", result);
    }
}

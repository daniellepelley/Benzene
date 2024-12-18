using System;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Validation;
using Benzene.Aws.ApiGateway;
using Benzene.Core.DI;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandling;
using Xunit;

namespace Benzene.Test.Core.Core.DI;

public class RegistrationsTest
{
    public RegistrationsTest()
    {
        //To load this assembly
        new ApiGatewayContext(new APIGatewayProxyRequest());
    }

    // [Fact]
    // public void RegistrationTest()
    // {
    //     var result = RegistrationErrorHandler.CheckType(typeof(IValidationSchemaBuilder));
    //
    //     Assert.Contains("Benzene.Core", result);
    //     Assert.Contains("IValidationSchemaBuilder", result);
    //     Assert.Contains(".UsingBenzene(x => x.AddBenzene())", result);
    // }

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

        // Assert.Contains("Benzene.Core", result);
        Assert.Contains("MessageRouter<>", result);
        // Assert.Contains(".UsingBenzene(x => x.AddMessageHandlers(<assemblies>))", result);

        Assert.Contains("Benzene.Aws.Core", result);
        Assert.Contains(".UsingBenzene(x => x.AddAwsMessageHandlers(<assemblies>))", result);
    }
    
    [Fact]
    public void RegistrationTest_Exception_UnableToResolve()
    {
        var result = RegistrationErrorHandler.CheckException(new Exception("Unable to resolve service for type 'Benzene.Abstractions.Mappers.IMessageTopicMapper`1[Benzene.Aws.ApiGateway.ApiGatewayContext]' while attempting to activate 'Benzene.Core.Mappers.MessageMapper`1[Benzene.Aws.ApiGateway.ApiGateway.ApiGatewayContext]'."));

        Assert.Contains("Benzene.Aws.ApiGateway", result);
        Assert.Contains("Benzene.Abstractions.Mappers.IMessageTopicMapper<Benzene.Aws.ApiGateway.ApiGatewayContext>", result);
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

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

    [Fact]
    public void RegistrationTest_Exception_ThirdPartyContainer_UnquotedMessage()
    {
        // A container other than Microsoft DI / Autofac phrases its message differently and may not
        // quote the type at all (this is SimpleInjector-shaped). The scan is wording-agnostic, so the
        // hint still resolves rather than silently going missing.
        var result = RegistrationErrorHandler.CheckException(new Exception(
            "No registration for type Benzene.Abstractions.Middleware.IMiddlewareFactory could be found "
            + "and an implicit registration could not be made."));

        Assert.Contains("IMiddlewareFactory", result);
        Assert.Contains(".UsingBenzene(x => x.AddBenzene())", result);
    }

    [Fact]
    public void RegistrationTest_Exception_MalformedMessage_ReturnsEmpty_DoesNotThrow()
    {
        // A message that begins like Microsoft DI's but has no quoted type used to make the old parser
        // throw IndexOutOfRange from Split('\'')[1] - inside the resolver's catch block, which then
        // replaced the real DI error. It must now degrade to an empty hint, never throw.
        var result = RegistrationErrorHandler.CheckException(new Exception("Unable to resolve service for type"));

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Describe_PrefersRequestedType_RegardlessOfExceptionWording()
    {
        // The requested type is always known, so guidance works even when the exception message is
        // unrecognizable (a third-party container, a localized message, a changed wording).
        var result = RegistrationErrorHandler.Describe(typeof(IMiddlewareFactory), new Exception("something inscrutable"));

        Assert.Contains("IMiddlewareFactory", result);
        Assert.Contains(".UsingBenzene(x => x.AddBenzene())", result);
    }

    [Fact]
    public void Describe_FallsBackToExceptionScan_ForATransitiveMissingDependency()
    {
        // The requested type itself is fine (not a Benzene registration), but its construction needed a
        // Benzene type the container names in the message - Describe still surfaces that.
        var result = RegistrationErrorHandler.Describe(typeof(string),
            new Exception("No service for type 'Benzene.Abstractions.Middleware.IMiddlewareFactory' has been registered."));

        Assert.Contains("IMiddlewareFactory", result);
        Assert.Contains(".UsingBenzene(x => x.AddBenzene())", result);
    }
}

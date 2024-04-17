using Benzene.CodeGen.Client;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Client;

public class MethodNameTest
{
    [Theory]
    [InlineData("tenant:create", "TenantCreate")]
    [InlineData("client:update:trigger", "ClientUpdateTrigger")]
    public void TopicMethodName(string topic, string expected)
    {
        Assert.Equal(expected, new TopicMethodName().Create(topic, new OpenApiSchema()));
    }
    
    [Theory]
    [InlineData("tenant:create", "CreateTenant")]
    [InlineData("client:update:trigger", "TriggerUpdateClient")]
    public void TopicReversedMethodName(string topic, string expected)
    {
        Assert.Equal(expected, new TopicReversedMethodName().Create(topic, new OpenApiSchema()));
    }
}

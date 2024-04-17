using System;
using Benzene.Abstractions.DI;
using Benzene.Clients;
using Moq;
using Xunit;

namespace Benzene.Test.Clients;

public class BenzeneMessageClientFactoryTest
{
    [Theory]
    [InlineData("tenantCore", "", false)]
    [InlineData("", "tenant:create", false)]
    [InlineData("tenantCore", "tenant:create", true)]
    [InlineData("TENANTCORE", "TENANT:CREATE", false)]
    [InlineData("tenantCore", "tenant:delete", false)]
    [InlineData("clientCore", "", true)]
    [InlineData("clientCore", null, true)]
    [InlineData("clientCore", "random:topic", true)]
    [InlineData("", "client:create", true)]
    [InlineData(null, "client:create", true)]
    [InlineData("randomService", "client:create", true)]
    [InlineData("randomService", "random:topic", false)]
    public void SendMessageAsync(string service, string topic, bool isFound)
    {
        var mockServiceResolver = new Mock<IServiceResolver>();
        var mockBenzeneMessageClient = new Mock<IBenzeneMessageClient>();
        var topicAndServiceKeys = new []
        {
            new TopicAndServiceKey("", "clientCore"),
            new TopicAndServiceKey("client:create", ""),
            new TopicAndServiceKey("tenant:create", "tenantCore"),
        };

        var clientMappings = new[]
        {
            new ClientMapping(topicAndServiceKeys, resolver => mockBenzeneMessageClient.Object),
        };

        var benzeneMessageClientFactory = new BenzeneMessageClientFactory(clientMappings, mockServiceResolver.Object);

        if (isFound)
        {
            var client = benzeneMessageClientFactory.Create(service, topic);
            Assert.NotNull(client);
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => benzeneMessageClientFactory.Create(service, topic));
        }
    }
}

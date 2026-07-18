using System;

namespace Benzene.Examples.App.Model.Messages;

/// <summary>
/// The integration event published (egress) after an order is created - see
/// <c>PublishOrderCreatedMessageHandler</c> in the AWS and Azure example hosts for where this is
/// sent, and <c>MessageTopicNames.OrderCreated</c> for the topic it's sent on.
/// </summary>
public class OrderCreatedEvent
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

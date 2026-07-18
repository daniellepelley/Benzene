namespace Benzene.Examples.App.Handlers;

public static class MessageTopicNames
{
    public const string OrderCreate = "order_create";
    public const string OrderGet = "order_get";
    public const string OrderGetAll = "order_getall";
    public const string OrderUpdate = "order_update";
    public const string OrderDelete = "order_delete";

    /// <summary>
    /// The egress topic <c>OrderCreatedEvent</c> is published on - see
    /// <c>PublishOrderCreatedMessageHandler</c> in the AWS and Azure example hosts.
    /// </summary>
    public const string OrderCreated = "order_created";
}
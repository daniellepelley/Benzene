using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;
using RabbitMQ.Client.Events;

namespace Benzene.RabbitMq.RabbitMqMessage;

/// <summary>
/// Processes a single RabbitMQ delivery by mapping it to a <see cref="RabbitMqContext"/> and running
/// it through the middleware pipeline in its own service scope, tagging the transport as
/// <c>"rabbitmq"</c> for the duration. Returns the handler's recorded <see cref="IMessageResult"/>
/// (possibly <c>null</c> if nothing set one), which <see cref="RabbitMqWorker"/> reads to support
/// <see cref="RabbitMqAckMode.Explicit"/>.
/// </summary>
public class RabbitMqApplication : MiddlewareApplication<BasicDeliverEventArgs, RabbitMqContext, IMessageResult?>
{
    /// <summary>Initializes a new instance of the <see cref="RabbitMqApplication"/> class.</summary>
    /// <param name="pipeline">The built RabbitMQ middleware pipeline to run each delivery through.</param>
    public RabbitMqApplication(IMiddlewarePipeline<RabbitMqContext> pipeline)
        : base(new TransportMiddlewarePipeline<RabbitMqContext>(TransportNames.RabbitMq, pipeline),
            RabbitMqContext.CreateInstance,
            context => context.MessageResult)
    { }
}

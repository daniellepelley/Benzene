using Benzene.Clients;
using Benzene.Core.Logging;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Kafka.Core.Kafka;
using Benzene.Microsoft.Dependencies;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;

var SomeStatus = "some-status";
var SomeName = "some-name";

var services = new ServiceCollection();

services.UsingBenzene(x => x.AddBenzene().AddBenzeneMiddleware());


var serviceContainer = new MicrosoftBenzeneServiceContainer(services);
ProducerConfig producerConfig = new()
{
    BootstrapServers = "localhost:9092",
    SaslMechanism = SaslMechanism.Plain,
    SecurityProtocol = SecurityProtocol.Plaintext
};

        
var producer = new ProducerBuilder<string, string>(producerConfig).Build();

var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<KafkaSendMessageContext>(serviceContainer);
var middlewarePipeline = middlewarePipelineBuilder
    .UseKafkaClient(producer)
    .Build();

var kafkaClient = new KafkaBenzeneMessageClient(middlewarePipeline, BenzeneLogger.NullLogger, serviceContainer.CreateServiceResolverFactory().CreateScope());

for(var i = 0; i < 5; i++)
{       
    var message = new { Status = SomeStatus, Name = SomeName };

    await kafkaClient.SendMessageAsync<object, Benzene.Abstractions.Results.Void>("order_create", message);
    await Task.Delay(1);
}

using Benzene.Core.MiddlewareBuilder;
using Benzene.FluentValidation;
using Benzene.Kafka.Core;
using Benzene.Kafka.Core.KafkaMessage;
using Benzene.Microsoft.Dependencies;
using Confluent.Kafka;
using FluentValidation;
using FluentValidation;

namespace Benzene.Examples.Kakfa;

public class Worker : BackgroundService
{
    private readonly BenzeneKafkaConsumer _consumer;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        
        
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            SaslMechanism = SaslMechanism.Plain,
            SecurityProtocol = SecurityProtocol.Plaintext,
            GroupId = Guid.NewGuid().ToString(),
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        var serviceCollection =
            DependenciesBuilder.CreateServiceCollection(DependenciesBuilder.GetConfiguration());

        var pipeline = new MiddlewarePipelineBuilder<KafkaRecordContext<Ignore, string>>(new MicrosoftBenzeneServiceContainer(serviceCollection))
            .UseMessageRouter(router => router.UseFluentValidation());

        var app = new KafkaApplication<Ignore, string>(pipeline.AsPipeline());
        _consumer = new BenzeneKafkaConsumer(new MicrosoftServiceResolverFactory(serviceCollection), app, consumerConfig);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        await _consumer.Start(new[] { "order_create", "order_delete" }, stoppingToken);
    }

    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}
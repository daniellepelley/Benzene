using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Aws.Kafka;
using Benzene.Core.MessageHandling;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Benzene.Tools;
using Benzene.Tools.Aws;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Benzene.Test.Aws;

public class KafkaMessagePipelineTest
{
    [Fact]
    public async Task Send()
    {
        var services = ServiceResolverMother.CreateServiceCollection();
        services.AddTransient<ILogger<MessageRouter<KafkaContext>>>(_ =>
                        NullLogger<MessageRouter<KafkaContext>>.Instance)
                    .AddTransient<ILogger>(_ => NullLogger.Instance)
                    .UsingBenzene(x => x
                        .AddKafka());

        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();

        var pipelineBuilder = new MiddlewarePipelineBuilder<KafkaContext>(new MicrosoftBenzeneServiceContainer(services));

        IMessageResult messageResult = null;

        pipelineBuilder
            .OnResponse("Check Response", context =>
            {
                messageResult = context.MessageResult;
            })
            .UseMessageRouter();

        var aws = new KafkaLambdaHandler(new KafkaApplication(pipelineBuilder.Build()), serviceResolver);

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsAwsKafkaEvent();

        await aws.HandleAsync(request.AwsEventStreamContext(), () => Task.CompletedTask);

        Assert.Equal(ServiceResultStatus.Ok, messageResult.Status);

    }

    [Fact]
    public async Task Send_UnprocessableEntity()
    {
        var services = ServiceResolverMother.CreateServiceCollection();
        services
            .AddTransient<ILogger<MessageRouter<KafkaContext>>>(_ =>
                NullLogger<MessageRouter<KafkaContext>>.Instance)
            .AddTransient<ILogger>(_ => NullLogger.Instance)
            .UsingBenzene(x => x
                .AddKafka());

        var pipeline = new MiddlewarePipelineBuilder<KafkaContext>(new MicrosoftBenzeneServiceContainer(services));

        IMessageResult messageResult = null;

        pipeline
            .OnResponse("Check Response", context =>
            {
                messageResult = context.MessageResult;
            })
            .UseMessageRouter();

        var aws = new KafkaApplication(pipeline.Build());

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsAwsKafkaEvent();

        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();
        await aws.HandleAsync(request, serviceResolver);
        Assert.Equal(ServiceResultStatus.Ok, messageResult.Status);
    }

    [Fact]
    public async Task Send_FromStream()
    {
        KafkaContext kafkaContext = null;
        var services = ServiceResolverMother.CreateServiceCollection();
        var app = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(services));

        app.UseKafka(message => message
            .Use(null, (context, next) =>
            {
                kafkaContext = context;
                return next();
            })
        );

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsAwsKafkaEvent();
        await app.Build().HandleAsync(AwsEventStreamContextBuilder.Build(request), new MicrosoftServiceResolverAdapter(services.BuildServiceProvider()));

        Assert.Equal(Defaults.Message, AwsLambdaBenzeneTestHost.StreamToString(kafkaContext.KafkaEvent.Records.First().Value.First().Value));
    }
}

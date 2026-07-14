using System.Threading.Tasks;
using Benzene.Abstractions;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.DynamoDb;
using Benzene.Aws.Lambda.DynamoDb.TestHelpers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Results;
using Benzene.Test.Examples;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Benzene.Test.Aws.DynamoDb;

[Message("example-orders:INSERT")]
public class ExampleOrderInsertedHandler : IMessageHandler<ExampleRequestPayload, Void>
{
    public Task<IBenzeneResult<Void>> HandleAsync(ExampleRequestPayload request)
    {
        return Task.FromResult(BenzeneResult.Ok(new Void()));
    }
}

public class DynamoDbMessagePipelineTest
{
    private static IBenzeneTestHost BuildHost(System.Action<DynamoDbRecordContext> onResponse)
    {
        return new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .AddTransient<ILogger<MessageRouter<DynamoDbRecordContext>>>(_ => NullLogger<MessageRouter<DynamoDbRecordContext>>.Instance)
                .AddTransient<ILogger>(_ => NullLogger.Instance)
                .UsingBenzene(x => x
                    .AddBenzene()
                    .AddDynamoDb())
            )
            .Configure(app => app
                .UseDynamoDb(dynamoDb => dynamoDb
                    .OnResponse("Check Response", onResponse)
                    .UseMessageHandlers()
                )
            ).BuildHost();
    }

    [Fact]
    public async Task Send()
    {
        bool? isSuccessful = null;
        var host = BuildHost(context => isSuccessful = context.IsSuccessful);

        var request = MessageBuilder
            .Create("example-orders:INSERT", new ExampleRequestPayload { Name = "some-name" })
            .AsDynamoDb();

        var batchResponse = await host.SendDynamoDbAsync(request);

        Assert.True(isSuccessful);
        Assert.Empty(batchResponse.BatchItemFailures);
    }

    [Fact]
    public async Task Send_UnknownTopic_ReportsSequenceNumberAsBatchFailure()
    {
        bool? isSuccessful = null;
        var host = BuildHost(context => isSuccessful = context.IsSuccessful);

        var request = MessageBuilder
            .Create("example-orders:UNKNOWN", new ExampleRequestPayload { Name = "some-name" })
            .AsDynamoDb();

        var batchResponse = await host.SendDynamoDbAsync(request);

        Assert.False(isSuccessful);
        var failure = Assert.Single(batchResponse.BatchItemFailures);
        Assert.Equal("1", failure.ItemIdentifier);
    }
}

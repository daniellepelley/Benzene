using System;
using Benzene.Results;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Aws.Lambda.DynamoDb;
using Benzene.Aws.Lambda.DynamoDb.TestHelpers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Benzene.Test.Aws.DynamoDb;

public class DynamoDbLambdaHandlerTest
{
    [Fact]
    public async Task DynamoDbPayload_IsHandled()
    {
        DynamoDbRecordContext handledContext = null;

        var app = new MiddlewarePipelineBuilder<AwsEventStreamContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));
        app.UseDynamoDb(dynamoDb => dynamoDb
            .Use(null, (context, next) =>
            {
                handledContext = context;
                return next();
            })
        );

        var request = MessageBuilder.Create("example-orders:INSERT", Defaults.MessageAsObject).AsDynamoDb();

        await app.Build().HandleAsync(AwsEventStreamContextBuilder.Build(request), ServiceResolverMother.CreateServiceResolver());

        Assert.NotNull(handledContext);
        Assert.Equal("INSERT", handledContext.Record.EventName);
        Assert.Equal("example-orders", DynamoDbUtils.GetTableName(handledContext.Record.EventSourceArn));
    }

    [Fact]
    public async Task NonDynamoDbPayload_FallsThroughToNextMiddleware()
    {
        var dynamoDbHandled = false;
        var fellThrough = false;

        var app = new MiddlewarePipelineBuilder<AwsEventStreamContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));
        app.UseDynamoDb(dynamoDb => dynamoDb
            .Use(null, (context, next) =>
            {
                dynamoDbHandled = true;
                return next();
            })
        );
        app.Use(null, (context, next) =>
        {
            fellThrough = true;
            return next();
        });

        // A BenzeneMessage-shaped payload: has a topic, but no dynamodb records.
        var request = new { topic = Defaults.Topic, headers = new { }, body = "{}" };

        await app.Build().HandleAsync(AwsEventStreamContextBuilder.Build(request), ServiceResolverMother.CreateServiceResolver());

        Assert.False(dynamoDbHandled);
        Assert.True(fellThrough);
    }

    [Fact]
    public async Task Application_ProcessesRecordsSequentiallyAndStopsAtFirstFailure()
    {
        var processed = new List<string>();

        var pipeline = new MiddlewarePipelineBuilder<DynamoDbRecordContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));
        pipeline.Use(null, (context, next) =>
        {
            processed.Add(context.Record.Dynamodb.SequenceNumber);
            context.MessageResult = (context.Record.Dynamodb.SequenceNumber != "2" ? BenzeneResult.Ok() : BenzeneResult.UnexpectedError());
            return next();
        });

        var application = new DynamoDbApplication(pipeline.Build());

        var request = MessageBuilder.Create("example-orders:INSERT", Defaults.MessageAsObject).AsDynamoDb(3);

        var response = await application.HandleAsync(request, ServiceResolverMother.CreateServiceResolverFactory());

        // Record 3 must never be processed: the stream is ordered CDC, so processing stops at the
        // first failure and Lambda redelivers from there.
        Assert.Equal(new[] { "1", "2" }, processed);
        var failure = Assert.Single(response.BatchItemFailures);
        Assert.Equal("2", failure.ItemIdentifier);
    }

    [Fact]
    public async Task Application_NullResult_IsReportedAsFailure_NotSilentlySkipped()
    {
        var processed = new List<string>();

        // A pipeline that never sets IsSuccessful (e.g. a short-circuit before the result setter) must
        // NOT let the ordered CDC stream advance past that record - that would silently skip it.
        var pipeline = new MiddlewarePipelineBuilder<DynamoDbRecordContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));
        pipeline.Use(null, (context, next) =>
        {
            processed.Add(context.Record.Dynamodb.SequenceNumber);
            return next(); // leaves context.MessageResult?.IsSuccessful null on record "2" onward
        });

        var application = new DynamoDbApplication(pipeline.Build());
        var request = MessageBuilder.Create("example-orders:INSERT", Defaults.MessageAsObject).AsDynamoDb(3);

        var response = await application.HandleAsync(request, ServiceResolverMother.CreateServiceResolverFactory());

        // Stops at the first record (null → failure), reporting it for redelivery rather than skipping.
        Assert.Equal(new[] { "1" }, processed);
        var failure = Assert.Single(response.BatchItemFailures);
        Assert.Equal("1", failure.ItemIdentifier);
    }

    [Fact]
    public async Task Application_MiddlewareThrows_ReportsFailureInsteadOfThrowing()
    {
        var services = ServiceResolverMother.CreateServiceCollection();
        services.AddTransient<ILogger<DynamoDbApplication>>(_ => NullLogger<DynamoDbApplication>.Instance);

        var pipeline = new MiddlewarePipelineBuilder<DynamoDbRecordContext>(new MicrosoftBenzeneServiceContainer(services));
        pipeline.Use(null, (context, next) => throw new Exception("boom"));

        var application = new DynamoDbApplication(pipeline.Build());

        var request = MessageBuilder.Create("example-orders:INSERT", Defaults.MessageAsObject).AsDynamoDb();

        var response = await application.HandleAsync(request, new MicrosoftServiceResolverFactory(services));

        var failure = Assert.Single(response.BatchItemFailures);
        Assert.Equal("1", failure.ItemIdentifier);
    }
}

﻿using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Sns;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseSns(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<SnsRecordContext>> action)
    {
        app.Register(x => x.AddSns());
        var pipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(resolver => new SnsLambdaHandler(new SnsApplication(pipeline), resolver));
    }
}

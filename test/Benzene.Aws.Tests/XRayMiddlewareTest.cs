using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Sampling;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.XRay;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Aws.Tests;

public class XRayMiddlewareTest
{
    [Fact]
    public async Task AddXRayTracing_WrapsEachMiddlewareInASubsegmentNamedAfterIt()
    {
        var recorder = AWSXRayRecorder.Instance;
        var capturedNames = new List<string>();

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        container.AddXRayTracing();

        var builder = new MiddlewarePipelineBuilder<object>(container);
        // Each middleware records the name of the X-Ray entity that is current while it runs - which,
        // if the wrapper worked, is the per-middleware subsegment named after it.
        builder.Use("first", (_, next) => { capturedNames.Add(recorder.GetEntity().Name); return next(); });
        builder.Use("second", (_, next) => { capturedNames.Add(recorder.GetEntity().Name); return next(); });

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        // Stand in for the segment the Lambda runtime opens per invocation.
        // Force a sampled segment: the default strategy rate-limits, so back-to-back test segments
        // would otherwise go unsampled and their subsegments would be dropped.
        recorder.BeginSegment("test", null, null, new SamplingResponse(SampleDecision.Sampled));
        try
        {
            await pipeline.HandleAsync(new object(), resolver);
        }
        finally
        {
            recorder.EndSegment();
        }

        Assert.Equal(new[] { "first", "second" }, capturedNames);
    }

    [Fact]
    public void AddXRayTracing_IsIdempotent_RegistersTheWrapperOnce()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.AddXRayTracing();
        container.AddXRayTracing();

        // The registration guard means the wrapper is only ever registered once as an
        // IMiddlewareWrapper, so a middleware is never double-wrapped (the factory resolves
        // IEnumerable<IMiddlewareWrapper>).
        Assert.Single(services.Where(d =>
            d.ServiceType == typeof(IMiddlewareWrapper) &&
            d.ImplementationType == typeof(XRayMiddlewareWrapper)));
    }

    [Fact]
    public async Task AddXRayTracing_RecordsTheExceptionOnAThrowingMiddleware_AndRethrows()
    {
        var recorder = AWSXRayRecorder.Instance;
        Entity? captured = null;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        container.AddXRayTracing();

        var builder = new MiddlewarePipelineBuilder<object>(container);
        builder.Use("boom", (_, _) =>
        {
            captured = recorder.GetEntity(); // the "boom" subsegment, still current at throw time
            throw new InvalidOperationException("kaboom");
        });

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        // Force a sampled segment: the default strategy rate-limits, so back-to-back test segments
        // would otherwise go unsampled and their subsegments would be dropped.
        recorder.BeginSegment("test", null, null, new SamplingResponse(SampleDecision.Sampled));
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.HandleAsync(new object(), resolver));
        }
        finally
        {
            recorder.EndSegment();
        }

        // The exception both propagates (asserted above) and is recorded on the failing stage's
        // subsegment as a fault, so an X-Ray trace can point at the middleware that threw.
        Assert.NotNull(captured);
        Assert.Equal("boom", captured!.Name);
        Assert.True(captured.HasFault);
    }

    [Fact]
    public async Task AddXRayTracing_IsANoOpWithNoActiveSegment()
    {
        // Off Lambda (no X-Ray segment in context) the decorator must not throw - it just runs the
        // pipeline, mirroring how ActivitySource.StartActivity returns null with no listener.
        var ran = false;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        container.AddXRayTracing();

        var builder = new MiddlewarePipelineBuilder<object>(container);
        builder.Use("solo", (_, next) => { ran = true; return next(); });

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        // No BeginSegment: there is no X-Ray context at all.
        await pipeline.HandleAsync(new object(), resolver);

        Assert.True(ran);
    }
}

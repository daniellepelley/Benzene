using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.TestPayloads;

namespace Benzene.Aws.Lambda.TestPayloads;

/// <summary>DI/pipeline wiring for the AWS transport dressers.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers the AWS transport dressers (SNS, SQS, API Gateway) so the <c>test-payloads</c> endpoint
    /// serves each domain topic's example dressed for those transports alongside the portable
    /// benzene-message envelope. Compose with <c>UseTestPayloads()</c>, or use
    /// <see cref="UseAwsTestPayloads{TContext}"/> for both in one call.
    /// </summary>
    public static IBenzeneServiceContainer AddAwsTestPayloadDressers(this IBenzeneServiceContainer services)
    {
        services.AddSingleton<ITestPayloadDresser>(_ => new SnsTestPayloadDresser());
        services.AddSingleton<ITestPayloadDresser>(_ => new SqsTestPayloadDresser());
        services.AddSingleton<ITestPayloadDresser>(_ => new ApiGatewayTestPayloadDresser());
        return services;
    }

    /// <summary>
    /// Opt-in one-call wiring: registers the <c>test-payloads</c> handler and the AWS SNS/SQS/API-Gateway
    /// dressers, so a deployed Lambda self-serves ready-to-fire example payloads dressed for the transports
    /// it is wired to. Opt-in by design - nothing is exposed unless this is called, and it reveals no more
    /// than the already-public <c>spec</c> topic.
    /// </summary>
    public static IMiddlewarePipelineBuilder<TContext> UseAwsTestPayloads<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app)
    {
        app.UseTestPayloads();
        app.Register(x => x.AddAwsTestPayloadDressers());
        return app;
    }
}

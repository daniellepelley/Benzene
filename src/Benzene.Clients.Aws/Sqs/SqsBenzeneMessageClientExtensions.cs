using System;
using Amazon.SQS;
using Benzene.Abstractions.DI;
using Microsoft.Extensions.Logging;

namespace Benzene.Clients.Aws.Sqs;

/// <summary>
/// Provides extension methods for registering <see cref="SqsBenzeneMessageClient"/> instances on a
/// <see cref="ClientsBuilder"/>.
///
/// Superseded by <c>.UseSqs(queueUrl)</c> on an <c>OutboundRoutingBuilder.Route</c> pipeline. See
/// <c>work/benzene-clients-redesign-plan.md</c>.
/// </summary>
[Obsolete("Use .UseSqs(queueUrl) on an OutboundRoutingBuilder.Route pipeline instead - see work/benzene-clients-redesign-plan.md")]
public static class SqsBenzeneMessageClientExtensions
{
    /// <summary>
    /// Registers a named <see cref="SqsBenzeneMessageClient"/> targeting the given queue.
    /// </summary>
    /// <param name="source">The clients builder to register on.</param>
    /// <param name="name">The name to register the client under.</param>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="serviceResolver">The service resolver used to run the client's middleware pipeline.</param>
    /// <param name="action">A callback used to further configure the created client builder.</param>
    /// <returns>The clients builder, for chaining.</returns>
    public static ClientsBuilder CreateSqsBenzeneMessageClient(this ClientsBuilder source, string name, string queueUrl, IServiceResolver serviceResolver, Action<ClientBuilder> action)
    {
        var clientBuilder = new ClientBuilder(resolver =>
            new SqsBenzeneMessageClient(queueUrl, resolver.GetService<IAmazonSQS>(), resolver.GetService<ILogger<SqsBenzeneMessageClient>>(), serviceResolver));

        action(clientBuilder);
        source.WithMessageClient(name, clientBuilder.Build);

        return source;
    }

    /// <summary>
    /// Registers an unnamed (default) <see cref="SqsBenzeneMessageClient"/> targeting the given queue.
    /// </summary>
    /// <param name="source">The clients builder to register on.</param>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="serviceResolver">The service resolver used to run the client's middleware pipeline.</param>
    /// <param name="action">A callback used to further configure the created client builder.</param>
    public static void CreateSqsBenzeneMessageClient(this ClientsBuilder source, string queueUrl, IServiceResolver serviceResolver, Action<ClientBuilder> action)
    {
        var clientBuilder = new ClientBuilder(resolver =>
            new SqsBenzeneMessageClient(queueUrl, resolver.GetService<IAmazonSQS>(), resolver.GetService<ILogger<SqsBenzeneMessageClient>>(), serviceResolver));

        action(clientBuilder);
        source.WithMessageClient(string.Empty, clientBuilder.Build);
    }
}

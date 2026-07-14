using Benzene.Abstractions.DI;
using Benzene.Abstractions.Serialization;

namespace Benzene.Abstractions.MessageHandlers.Request;

/// <summary>
/// Startup-time configuration surface for request mapping: registers which
/// <see cref="ISerializer"/>(s) are available to deserialize incoming request bodies for a given
/// context type, and which one is the default. Backs multi-serializer request mappers (e.g. one
/// process that accepts both JSON and a binary format, selected per request via
/// <see cref="ISerializerOption{TContext}"/>).
/// </summary>
/// <typeparam name="TContext">The transport-specific context type requests are mapped from.</typeparam>
public interface IRequestMapBuilder<TContext>
{
    /// <summary>Registers additional DI dependencies alongside serializer configuration.</summary>
    /// <param name="action">An action that receives the service container for dependency registration.</param>
    void Register(Action<IBenzeneServiceContainer> action);

    /// <summary>Makes a serializer available for use, resolved from DI by type.</summary>
    /// <typeparam name="T">The serializer type to register and make available.</typeparam>
    /// <returns>This builder, to allow fluent chaining.</returns>
    IRequestMapBuilder<TContext> Use<T>() where T : class, ISerializer;

    /// <summary>Makes a serializer instance available for use.</summary>
    /// <param name="serializer">The serializer instance to make available.</param>
    /// <returns>This builder, to allow fluent chaining.</returns>
    IRequestMapBuilder<TContext> Use(ISerializer serializer);

    /// <summary>Makes a serializer available for use, intended as the process's default choice.</summary>
    /// <remarks>
    /// Exact fall-back precedence between this and <see cref="Use{T}"/> registrations is determined
    /// by the implementation (e.g. registration order and each option's <c>CanHandle</c> predicate) --
    /// check the concrete <see cref="IRequestMapBuilder{TContext}"/> and request-mapper implementation
    /// in use before relying on a specific priority.
    /// </remarks>
    /// <typeparam name="T">The serializer type to register and make available as the default.</typeparam>
    /// <returns>This builder, to allow fluent chaining.</returns>
    IRequestMapBuilder<TContext> UseDefault<T>() where T : class, ISerializer;
}
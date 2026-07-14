using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// Startup-time configuration surface for message routing: application code registers
/// <see cref="IHandlerMiddlewareBuilder"/>s here (typically via the DI container extension methods
/// that expose this builder) so an <see cref="IHandlerPipelineBuilder"/> can later include them in
/// every handler pipeline it builds. Also inherits <see cref="IRegisterDependency"/> so DI
/// registrations can be added alongside middleware registration in the same configuration step.
/// </summary>
public interface IMessageRouterBuilder : IRegisterDependency
{
    /// <summary>Registers a handler middleware builder to be included in future handler pipelines.</summary>
    /// <param name="handlerMiddlewareBuilder">The middleware builder to add.</param>
    /// <returns>This builder, to allow fluent chaining.</returns>
    IMessageRouterBuilder Add(IHandlerMiddlewareBuilder handlerMiddlewareBuilder);

    /// <summary>Returns every handler middleware builder registered so far.</summary>
    /// <returns>The registered handler middleware builders.</returns>
    IHandlerMiddlewareBuilder[] GetBuilders();
}
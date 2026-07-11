using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Middleware;

/// <summary>
/// Provides a fluent builder interface for constructing middleware pipelines.
/// </summary>
/// <typeparam name="TContext">The type of context object that will flow through the middleware pipeline.</typeparam>
/// <remarks>
/// The middleware pipeline builder implements the builder pattern to provide a fluent, readable API for composing middleware.
/// Middleware components are registered in the order they should execute. The builder also supports:
/// - Dependency injection integration through <see cref="IRegisterDependency"/>
/// - Creating sub-pipelines with different context types
/// - Deferred middleware instantiation via factory functions
/// Once built, the pipeline is immutable and can be executed multiple times.
/// </remarks>
public interface IMiddlewarePipelineBuilder<TContext> : IRegisterDependency
 {
     /// <summary>
     /// Adds a middleware component to the pipeline using a factory function.
     /// </summary>
     /// <param name="func">A factory function that creates the middleware instance, receiving a service resolver for dependency resolution.</param>
     /// <returns>The current builder instance for method chaining.</returns>
     /// <remarks>
     /// Middleware components are executed in the order they are registered. The factory function enables:
     /// - Lazy instantiation of middleware
     /// - Access to the dependency injection container via <paramref name="func"/>
     /// - Dynamic middleware creation based on runtime conditions
     /// </remarks>
     IMiddlewarePipelineBuilder<TContext> Use(Func<IServiceResolver, IMiddleware<TContext>> func);

     /// <summary>
     /// Creates a new pipeline builder for a different context type.
     /// </summary>
     /// <typeparam name="TNewContext">The context type for the new pipeline builder.</typeparam>
     /// <returns>A new pipeline builder instance that operates on the specified context type.</returns>
     /// <remarks>
     /// This method enables creation of sub-pipelines or parallel pipelines with different context types.
     /// It shares the same dependency registration infrastructure but creates an independent middleware chain.
     /// Use context converters (see <see cref="IContextConverter{TContextIn, TContextOut}"/>) to bridge between pipelines with different context types.
     /// </remarks>
     IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();

     /// <summary>
     /// Builds the middleware pipeline from the registered components.
     /// </summary>
     /// <returns>An immutable middleware pipeline ready for execution.</returns>
     /// <remarks>
     /// This method finalizes the pipeline construction and returns an executable pipeline.
     /// The returned pipeline is immutable and can be safely reused for multiple executions.
     /// Once built, no additional middleware can be added to this pipeline.
     /// </remarks>
     IMiddlewarePipeline<TContext> Build();
 }

using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;

namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// The per-invocation context flowing through the handler middleware pipeline (see
/// <see cref="IHandlerPipelineBuilder"/> / <see cref="IHandlerMiddlewareBuilder"/>) for a single
/// message handler call. This is the message-handler equivalent of a transport context (e.g.
/// <c>IHttpContext</c>): it carries the already-mapped, strongly-typed request in and the
/// handler's result out, so handler middleware (validation, logging, filters, etc.) can inspect
/// or replace either side without knowing about the underlying transport.
/// </summary>
/// <typeparam name="TRequest">The strongly-typed request handled by this pipeline invocation.</typeparam>
/// <typeparam name="TResponse">The strongly-typed response payload produced by the handler.</typeparam>
public interface IMessageHandlerContext<TRequest, TResponse>
{
    /// <summary>The topic (id + version) the incoming message was routed on.</summary>
    ITopic Topic { get; }

    /// <summary>The concrete handler type resolved for this invocation, or <c>null</c> if not yet resolved.</summary>
    Type? HandlerType { get; }

    /// <summary>The strongly-typed request, already mapped from the transport payload.</summary>
    TRequest Request { get; }

    /// <summary>
    /// The result produced by the handler pipeline. Set by <c>MessageHandlerMiddleware</c> after
    /// the inner handler runs; middleware earlier in the pipeline can also assign this to short-circuit
    /// the remaining pipeline (e.g. to return a validation failure without invoking the handler).
    /// </summary>
    IBenzeneResult<TResponse> Response { get; set; }
}
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Results;
using JsonSerializer = Benzene.Core.MessageHandlers.Serialization.JsonSerializer;

namespace Benzene.Http.BenzeneMessage;

/// <summary>
/// Transport-agnostic HTTP middleware that dispatches a POSTed BenzeneMessage envelope
/// (<c>{ "topic": ..., "headers": ..., "body": ... }</c>) into a BenzeneMessage pipeline — the
/// HTTP equivalent of the direct AWS Lambda invoke path. Works on any Benzene HTTP transport
/// (API Gateway, Azure Functions, ASP.NET Core, self-host) because it drives the transport-neutral
/// request/response adapters directly, the same short-circuit shape as <c>SpecUiMiddleware</c> and
/// <c>CorsMiddleware</c>.
/// </summary>
/// <typeparam name="TContext">The HTTP context type.</typeparam>
/// <remarks>
/// On a POST to its path it runs the envelope through the pipeline (topic routing, validation,
/// middleware, handler — the <c>"benzene"</c> transport) and writes the response envelope
/// (<c>{ "statusCode": ..., "headers": ..., "body": ... }</c>) as <c>application/json</c>, with
/// the HTTP status mapped from the envelope's status via <see cref="IHttpStatusCodeMapper"/>.
/// Any other request falls through to <c>next</c>. The envelope is always read and written with
/// Benzene's default JSON serialization, regardless of the payload formats the app's own
/// serializer negotiates.
/// <para>
/// Security: this endpoint exposes every topic the pipeline routes — including topics with no
/// HTTP mapping — so it is opt-in and intended for local development or protected/admin
/// environments. Restrict topics with <see cref="BenzeneMessageHttpOptions.TopicFilter"/> and
/// compose authentication middleware in front of it; do not expose it unauthenticated in
/// production.
/// </para>
/// </remarks>
public class BenzeneMessageHttpMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext
{
    private static readonly JsonSerializer Serializer = new();

    private readonly string _path;
    private readonly Func<string, bool>? _topicFilter;
    private readonly BenzeneMessageApplication _application;
    private readonly IServiceResolver _serviceResolver;
    private readonly IHttpRequestAdapter<TContext> _httpRequestAdapter;
    private readonly IMessageBodyGetter<TContext> _messageBodyGetter;
    private readonly IBenzeneResponseAdapter<TContext> _responseAdapter;
    private readonly IHttpStatusCodeMapper _httpStatusCodeMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneMessageHttpMiddleware{TContext}"/> class.
    /// </summary>
    /// <param name="options">The endpoint options (path and optional topic filter).</param>
    /// <param name="pipeline">The built BenzeneMessage pipeline to dispatch envelopes to.</param>
    /// <param name="serviceResolver">The service resolver for the current invocation scope.</param>
    /// <param name="httpRequestAdapter">Adapter used to read the request method and path.</param>
    /// <param name="messageBodyGetter">Getter used to read the request body.</param>
    /// <param name="responseAdapter">Adapter used to write the response.</param>
    /// <param name="httpStatusCodeMapper">Maps the envelope's status to an HTTP status code.</param>
    public BenzeneMessageHttpMiddleware(
        BenzeneMessageHttpOptions options,
        IMiddlewarePipeline<BenzeneMessageContext> pipeline,
        IServiceResolver serviceResolver,
        IHttpRequestAdapter<TContext> httpRequestAdapter,
        IMessageBodyGetter<TContext> messageBodyGetter,
        IBenzeneResponseAdapter<TContext> responseAdapter,
        IHttpStatusCodeMapper httpStatusCodeMapper)
    {
        _path = NormalizePath(options.Path);
        _topicFilter = options.TopicFilter;
        _application = new BenzeneMessageApplication(pipeline);
        _serviceResolver = serviceResolver;
        _httpRequestAdapter = httpRequestAdapter;
        _messageBodyGetter = messageBodyGetter;
        _responseAdapter = responseAdapter;
        _httpStatusCodeMapper = httpStatusCodeMapper;
    }

    /// <summary>
    /// Gets the name of the middleware.
    /// </summary>
    public string Name => "BenzeneMessageHttp";

    /// <summary>
    /// Dispatches a matching POSTed envelope into the BenzeneMessage pipeline and short-circuits;
    /// any other request is passed to the next middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var request = _httpRequestAdapter.Map(context);

        if (!string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase) ||
            NormalizePath(request.Path) != _path)
        {
            await next();
            return;
        }

        var response = await DispatchAsync(context);

        _responseAdapter.SetStatusCode(context, _httpStatusCodeMapper.Map(response.StatusCode));
        _responseAdapter.SetContentType(context, "application/json; charset=utf-8");
        _responseAdapter.SetBody(context, Serializer.Serialize(response));
        await _responseAdapter.FinalizeAsync(context);
    }

    private async Task<IBenzeneMessageResponse> DispatchAsync(TContext context)
    {
        BenzeneMessageRequest? benzeneMessageRequest;
        try
        {
            var body = _messageBodyGetter.GetBody(context);
            benzeneMessageRequest = string.IsNullOrWhiteSpace(body)
                ? null
                : Serializer.Deserialize<BenzeneMessageRequest>(body);
        }
        catch
        {
            return ErrorResponse(BenzeneResultStatus.BadRequest,
                "The request body is not a valid BenzeneMessage envelope");
        }

        if (string.IsNullOrWhiteSpace(benzeneMessageRequest?.Topic))
        {
            return ErrorResponse(BenzeneResultStatus.BadRequest,
                "The request body must be a BenzeneMessage envelope with a topic");
        }

        if (_topicFilter != null && !_topicFilter(benzeneMessageRequest!.Topic))
        {
            return ErrorResponse(BenzeneResultStatus.NotFound,
                $"Topic '{benzeneMessageRequest!.Topic}' is not available on this endpoint");
        }

        return await _application.HandleAsync(benzeneMessageRequest!,
            _serviceResolver.GetService<IServiceResolverFactory>());
    }

    private static IBenzeneMessageResponse ErrorResponse(string statusCode, string message)
    {
        return new BenzeneMessageResponse
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>(),
            Body = Serializer.Serialize(new ErrorMessage(message))
        };
    }

    private record ErrorMessage(string Message);

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var trimmed = path.Trim();
        if (!trimmed.StartsWith('/'))
        {
            trimmed = "/" + trimmed;
        }

        if (trimmed.Length > 1 && trimmed.EndsWith('/'))
        {
            trimmed = trimmed.TrimEnd('/');
        }

        return trimmed.ToLowerInvariant();
    }
}

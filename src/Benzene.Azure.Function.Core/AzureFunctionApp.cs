using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Exceptions;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.Core;

/// <summary>
/// Default implementation of <see cref="IAzureFunctionApp"/>. Dispatches a request to whichever of its
/// constructed entry point applications matches the request (and response, where applicable) type.
/// </summary>
public class AzureFunctionApp : IAzureFunctionApp
{
    private readonly (string? Key, IEntryPointMiddlewareApplication App)[] _apps;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFunctionApp"/> class, constructing every
    /// registered entry point application for the current invocation scope.
    /// </summary>
    /// <param name="appBuilders">The (optional key, factory) pairs for each entry point application registered with the builder.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to construct each entry point application.</param>
    public AzureFunctionApp((string? Key, Func<IServiceResolverFactory, IEntryPointMiddlewareApplication> Factory)[] appBuilders, IServiceResolverFactory serviceResolverFactory)
    {
        _apps = appBuilders.Select(x => (x.Key, x.Factory(serviceResolverFactory))).ToArray();
    }

    /// <summary>
    /// Handles a request that expects a response, dispatching to the registered entry point application
    /// whose request/response types match (and whose discriminator key equals <paramref name="name"/>,
    /// when one is given).
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request to handle.</param>
    /// <param name="name">The discriminator key to match, or <c>null</c> for the first type-only match.</param>
    /// <returns>A task that resolves to the response produced by the matching entry point application.</returns>
    /// <exception cref="BenzeneException">Thrown when no registered entry point application matches.</exception>
    public Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request, string? name = null)
    {
        foreach (var (key, app) in _apps)
        {
            if ((name == null || key == name) && app is EntryPointMiddlewareApplication<TRequest, TResponse> typed)
            {
                return typed.SendAsync(request);
            }
        }

        throw CreateNoEntryPointException($"{FormatType(typeof(TRequest))} -> {FormatType(typeof(TResponse))}", name);
    }

    /// <summary>
    /// Handles a fire-and-forget request, dispatching to the registered entry point application whose
    /// request type matches (and whose discriminator key equals <paramref name="name"/>, when given).
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request to handle.</param>
    /// <param name="name">The discriminator key to match, or <c>null</c> for the first type-only match.</param>
    /// <returns>A task that completes when the matching entry point application has finished handling the request.</returns>
    /// <exception cref="BenzeneException">Thrown when no registered entry point application matches.</exception>
    public Task HandleAsync<TRequest>(TRequest request, string? name = null)
    {
        foreach (var (key, app) in _apps)
        {
            if ((name == null || key == name) && app is IEntryPointMiddlewareApplication<TRequest> typed)
            {
                return typed.SendAsync(request);
            }
        }

        throw CreateNoEntryPointException(FormatType(typeof(TRequest)), name);
    }

    private BenzeneException CreateNoEntryPointException(string requestedShape, string? name)
    {
        var registered = _apps
            .SelectMany(entry => entry.App.GetType().GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericArguments().Length > 0 &&
                            typeof(IEntryPointMiddlewareApplication).IsAssignableFrom(i))
                .Select(i => (entry.Key == null ? "" : $"{entry.Key}:") +
                             string.Join(" -> ", i.GetGenericArguments().Select(FormatType))))
            .Distinct()
            .ToArray();

        var registeredList = registered.Length == 0 ? "none" : string.Join(", ", registered);
        var requested = name == null ? $"[{requestedShape}]" : $"[{name}:{requestedShape}]";
        return new BenzeneException(
            $"No entry point application is registered for request shape {requested}. " +
            $"Registered entry points: [{registeredList}]. " +
            "Wire the matching Use...() extension (UseHttp, UseServiceBus, UseEventHub, UseQueueStorage, ...) " +
            "in your StartUp's Configure method for this trigger's request type" +
            (name == null ? "." : ", and pass the same name to the trigger's Handle...(name, …) call."));
    }

    private static string FormatType(Type type)
    {
        if (type.IsArray)
        {
            return $"{FormatType(type.GetElementType()!)}[]";
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name[..type.Name.IndexOf('`')];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FormatType))}>";
    }
}

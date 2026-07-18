using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Azure.Function.Timer;

/// <summary>
/// Provides extension methods for registering timer message-handling services and adding timer
/// trigger handling to an <see cref="IAzureFunctionAppBuilder"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process timer-triggered ticks.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container, for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="UseTimerTrigger(IAzureFunctionAppBuilder, Action{IMiddlewarePipelineBuilder{TimerContext}})"/>; you don't normally need to call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddAzureTimer(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();
        services.TryAddScoped<PresetTopicHolder>();

        services.AddScoped<IMessageTopicGetter<TimerContext>>(resolver =>
            new PresetTopicMessageTopicGetter<TimerContext>(new TimerMessageTopicGetter(), resolver.GetService<PresetTopicHolder>()));
        services.AddScoped<IMessageVersionGetter<TimerContext>, HeaderMessageVersionGetter<TimerContext>>();
        services.AddScoped<IMessageHeadersGetter<TimerContext>, TimerMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<TimerContext>, TimerMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<TimerContext>, TimerMessageMessageHandlerResultSetter>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("timer"));
        return services;
    }

    /// <summary>
    /// Adds a timer entry point application to the Azure Function app, configuring its inner
    /// middleware pipeline. A tick carries no message, so either consume it directly with
    /// <c>UseTick(...)</c>, or - to run a scheduled job through the same message handlers as any
    /// other transport - give the pipeline the job's topic:
    /// <c>timer.UsePresetTopic("nightly-cleanup").UseMessageHandlers()</c>.
    /// </summary>
    /// <param name="app">The Azure Function app builder to add timer handling to.</param>
    /// <param name="action">The action that configures the timer middleware pipeline.</param>
    /// <returns>The Azure Function app builder, for method chaining.</returns>
    /// <remarks>
    /// Named <c>UseTimerTrigger</c> (not <c>UseTimer</c>) to avoid colliding with
    /// <c>Benzene.Diagnostics</c>'s timing middleware extension of that name.
    /// </remarks>
    public static IAzureFunctionAppBuilder UseTimerTrigger(this IAzureFunctionAppBuilder app, Action<IMiddlewarePipelineBuilder<TimerContext>> action)
    {
        app.Register(x => x.AddAzureTimer());
        var pipeline = app.Create<TimerContext>();
        action(pipeline);
        app.Add(serviceResolverFactory => new TimerApplication(pipeline.Build(), serviceResolverFactory));
        return app;
    }

    /// <summary>
    /// Applies timer-trigger configuration to a platform-neutral <see cref="IBenzeneApplicationBuilder"/>.
    /// No-op on any platform other than Azure Functions.
    /// </summary>
    /// <param name="app">The application builder passed to <c>BenzeneStartUp.Configure</c>.</param>
    /// <param name="action">The action that configures the timer middleware pipeline.</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IBenzeneApplicationBuilder UseTimerTrigger(this IBenzeneApplicationBuilder app, Action<IMiddlewarePipelineBuilder<TimerContext>> action)
    {
        if (app is IAzureFunctionAppBuilder azureApp)
        {
            azureApp.UseTimerTrigger(action);
        }
        return app;
    }
}

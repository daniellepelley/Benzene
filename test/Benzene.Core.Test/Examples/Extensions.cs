// using System;
// using Benzene.Abstractions.DI;
// using Benzene.Microsoft.Dependencies;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace Benzene.Test.Examples;
//
// public static class Extensions
// {
//     public static IServiceCollection UsingBenzene(this IServiceCollection services, Action<IBenzeneServiceContainer> action)
//     {
//         var microsoftBenzeneServiceContainer = new MicrosoftBenzeneServiceContainer(services);
//         services.AddScoped<IBenzeneServiceContainer>(_ => microsoftBenzeneServiceContainer);
//         action(microsoftBenzeneServiceContainer);
//         return services;
//     }
// }

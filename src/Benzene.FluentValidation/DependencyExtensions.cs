using System;
using System.Linq;
using System.Reflection;
using FluentValidation;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Validation;
using Benzene.Core.DI;
using Benzene.Core.Helper;
using Benzene.FluentValidation.Schema;

namespace Benzene.FluentValidation;

public static class DependencyExtensions
{
    public static IMessageRouterBuilder UseFluentValidation(this IMessageRouterBuilder builder, params Assembly[] assemblies)
    {
        builder.Register(x => x.AddFluentValidation(assemblies));
        return builder.Add(new ValidationMiddlewareBuilder());
    }
    
    public static IMessageRouterBuilder UseFluentValidation(this IMessageRouterBuilder builder, Type[] types)
    {
        builder.Register(x => x.AddFluentValidation(types));
        return builder.Add(new ValidationMiddlewareBuilder());
    }

    public static IBenzeneServiceContainer AddFluentValidation(this IBenzeneServiceContainer services, params Assembly[] assemblies)
    {
        return services.AddFluentValidation(Utils.GetAllTypes(assemblies).ToArray());
    }

    public static IBenzeneServiceContainer AddFluentValidation(this IBenzeneServiceContainer services, Type[] types)
    {
        var validatorTypes = types
            .Where(t => typeof(IValidator).IsAssignableFrom(t) && !t.IsAbstract &&
                        !t.Assembly.FullName.StartsWith("FluentValidation,"))
            .ToArray();

        foreach (var validatorType in validatorTypes)
        {
            services.TryAddSingleton(validatorType.GetInterface("IValidator`1"), validatorType);
        }
        
        var validators = validatorTypes
            .Select(x => 
                Activator.CreateInstance(x) as IValidator)
            .ToArray();

        services.TryAddSingleton<IValidationSchemaBuilder>(new FluentValidationSchemaBuilder(validators));
        return services;
    }
}

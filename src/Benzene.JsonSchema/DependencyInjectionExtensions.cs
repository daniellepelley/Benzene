using Benzene.Abstractions.DI;

namespace Benzene.JsonSchema;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddJsonSchema(this IBenzeneServiceContainer services)
    {
        services.AddScoped(typeof(IJsonSchemaProvider<>), typeof(DefaultJsonSchemaProvider<>));
        services.AddScoped(typeof(JsonSchemaMiddleware<>));
        return services;
    }
}
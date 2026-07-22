using Benzene.Abstractions.DI;

namespace Benzene.JsonSchema;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddJsonSchema(this IBenzeneServiceContainer services)
    {
        // TryAdd so a provider registered in ConfigureServices (a user's own, or
        // AddSuppliedJsonSchemas' below) reliably wins over this default - UseJsonSchema calls
        // this at pipeline wire-up, after ConfigureServices has run.
        services.TryAddScoped(typeof(IJsonSchemaProvider<>), typeof(DefaultJsonSchemaProvider<>));
        services.AddScoped(typeof(JsonSchemaMiddleware<>));
        return services;
    }

    /// <summary>
    /// Registers hand-authored (bring-your-own) JSON Schemas for request validation: topics whose
    /// request type is mapped in the catalog validate against the supplied schema; everything else
    /// falls back to the default generated-from-type schema. Call in ConfigureServices, before the
    /// pipeline's <c>UseJsonSchema()</c> runs.
    /// </summary>
    public static IBenzeneServiceContainer AddSuppliedJsonSchemas(this IBenzeneServiceContainer services,
        SuppliedJsonSchemaCatalog catalog)
    {
        services.AddSingleton(catalog);
        services.TryAddScoped(typeof(IJsonSchemaProvider<>), typeof(SuppliedJsonSchemaProvider<>));
        return services;
    }
}

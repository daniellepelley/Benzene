using Benzene.Abstractions.DI;
using Benzene.Core.Versioning.Casters;
using Benzene.Core.Versioning.Deserializer;

namespace Benzene.Core.Versioning.Schemas;

public interface ISchemaCaster
{
    SchemaCastDefinition Definition { get; }
    Type FromType { get; }
    Type ToType { get; }
}

public interface ISchemaCaster<TFrom, TTo> : ISchemaCaster
{
    ICaster<TFrom, TTo> Caster { get; }
}

public static class SchemaCasterExtensions
{
    public static IBenzeneServiceContainer RegisterDocumentSchemaVersionsDefinitions(this IBenzeneServiceContainer services, IEnumerable<PayloadSchemaVersions> documentVersionsDefinitions)
    {
        return services.AddSingleton<ISchemaCasters>(x =>
                new SchemaCasters(
                    new SchemaCastDefinitionsExpander().Expand(x.GetServices<ISchemaCaster>().ToArray(),
                        documentVersionsDefinitions.ToArray())));
    }

    public static IBenzeneServiceContainer RegisterSchemaCastDefinitions(this IBenzeneServiceContainer services, Action<SchemaCastersBuilder> action)
    {
        var builder = new SchemaCastersBuilder(services);

        action(builder);

        foreach (var schemaCastDefinition in builder.Build())
        {
            _ = services.AddSingleton(schemaCastDefinition);
        }

        return services;
    }
}

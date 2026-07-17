using Benzene.Core.Versioning.CasterBuilder;

namespace Benzene.Core.Versioning.Schemas;

public class SchemaCastersBuilder
{
    private readonly List<ISchemaCaster> _definitions = new();

    public SchemaCastersBuilder Add<TFrom, TTo>(string topic, string fromSchema, string toSchema,
        Action<CasterFactory<TFrom, TTo>> action)
    {
        var builder = new SchemaCasterBuilder<TFrom, TTo>(topic, fromSchema, toSchema);
        _ = builder.WithCustomCaster(action);
        _definitions.Add(builder.Build());
        return this;
    }

    public SchemaCastersBuilder Add<TFrom, TTo>(string topic, string fromSchema, string toSchema)
    {
        return Add<TFrom, TTo>(topic, fromSchema, toSchema, _ => { });
    }

    public ISchemaCaster[] Build()
    {
        return _definitions.ToArray();
    }
}

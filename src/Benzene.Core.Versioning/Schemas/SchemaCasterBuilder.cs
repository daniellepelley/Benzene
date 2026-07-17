using Benzene.Core.Versioning.CasterBuilder;
using Benzene.Core.Versioning.Casters;

namespace Benzene.Core.Versioning.Schemas;

public class SchemaCasterBuilder<TFrom, TTo>(string topic, string fromSchema, string toSchema)
{
    private ICaster<TFrom, TTo>? _caster;

    public SchemaCasterBuilder<TFrom, TTo> WithCustomCaster(
        Action<CasterFactory<TFrom, TTo>> action)
    {
        var castMapperFactory = new CasterFactory<TFrom, TTo>();
        action(castMapperFactory);
        _caster = castMapperFactory.Build();
        return this;
    }

    public SchemaCasterBuilder<TFrom, TTo> WithCustomCaster(
        ICaster<TFrom, TTo> caster)
    {
        _caster = caster;
        return this;
    }

    public ISchemaCaster Build()
    {
        try
        {
            if (_caster == null)
            {
                var castMapperFactory = new CasterFactory<TFrom, TTo>();
                _caster = castMapperFactory.Build();
            }

            return new SchemaCaster<TFrom, TTo>
            {
                Definition = new SchemaCastDefinition
                {
                    Topic = topic,
                    FromSchema = fromSchema,
                    ToSchema = toSchema
                },
                Caster = _caster
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to build caster for {topic}: {fromSchema} -> {toSchema}", ex);
        }
    }
}

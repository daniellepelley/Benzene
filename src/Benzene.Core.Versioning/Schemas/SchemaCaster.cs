using Benzene.Core.Versioning.Casters;

namespace Benzene.Core.Versioning.Schemas;

public class SchemaCaster<TFrom, TTo> : ISchemaCaster<TFrom, TTo>
{
    public required SchemaCastDefinition Definition { get; init; }
    public Type FromType => typeof(TFrom);
    public Type ToType => typeof(TTo);
    public required ICaster<TFrom, TTo> Caster { get; init; }

    /// <inheritdoc />
    public object? Cast(object from) => Caster.Cast((TFrom)from);
}

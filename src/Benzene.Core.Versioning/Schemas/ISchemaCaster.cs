using Benzene.Core.Versioning.Casters;

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

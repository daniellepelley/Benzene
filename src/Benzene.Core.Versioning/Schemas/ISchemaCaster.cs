using Benzene.Core.Versioning.Casters;

namespace Benzene.Core.Versioning.Schemas;

public interface ISchemaCaster
{
    SchemaCastDefinition Definition { get; }
    Type FromType { get; }
    Type ToType { get; }

    /// <summary>
    /// Casts a boxed <see cref="FromType"/> value to a boxed <see cref="ToType"/> value. Lets a caller
    /// that only knows the runtime types (e.g. a version-aware request/response mapper) invoke the
    /// strongly-typed <see cref="Casters.ICaster{TFrom,TTo}"/> without reflection.
    /// </summary>
    /// <param name="from">The value to cast; must be assignable to <see cref="FromType"/>.</param>
    /// <returns>The cast value, assignable to <see cref="ToType"/>.</returns>
    object? Cast(object from);
}

public interface ISchemaCaster<TFrom, TTo> : ISchemaCaster
{
    ICaster<TFrom, TTo> Caster { get; }
}

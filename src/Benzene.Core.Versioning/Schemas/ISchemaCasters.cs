using System.Diagnostics.CodeAnalysis;

namespace Benzene.Core.Versioning.Schemas;

public interface ISchemaCasters
{
    ISchemaCaster[] GetAll();

    ISchemaCaster GetSchemaCaster(string fromSchema, string toSchema, string topic);

    /// <summary>
    /// Finds the caster for <paramref name="topic"/> that converts from schema version
    /// <paramref name="fromSchema"/> into the CLR type <paramref name="toType"/>. Used on the request
    /// (upcast) path, where the incoming version is known as a string but the target is the handler's
    /// declared request type (docs/specification/versioning.md §4.1).
    /// </summary>
    /// <returns><c>true</c> and sets <paramref name="caster"/> if a matching caster exists; otherwise <c>false</c>.</returns>
    bool TryGetSchemaCaster(string topic, string fromSchema, Type toType, [NotNullWhen(true)] out ISchemaCaster? caster);

    /// <summary>
    /// Finds the caster for <paramref name="topic"/> that converts from the CLR type
    /// <paramref name="fromType"/> into schema version <paramref name="toSchema"/>. Used on the response
    /// (downcast) path, where the source is the handler's declared response type but the target is the
    /// requested version string (docs/specification/versioning.md §4.2).
    /// </summary>
    /// <returns><c>true</c> and sets <paramref name="caster"/> if a matching caster exists; otherwise <c>false</c>.</returns>
    bool TryGetSchemaCaster(string topic, Type fromType, string toSchema, [NotNullWhen(true)] out ISchemaCaster? caster);
}

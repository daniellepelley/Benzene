namespace Benzene.Core.Versioning.CasterBuilder;

public class CasterMapperSettings
{
    public required IReadOnlyDictionary<(Type, Type), Delegate> Funcs { init; get; }
    public required IReadOnlyDictionary<Type, Dictionary<string, Func<object?>>> InitValues { init; get; }
    public required IReadOnlyDictionary<Type, Type> TypeMapping { init; get; }
}

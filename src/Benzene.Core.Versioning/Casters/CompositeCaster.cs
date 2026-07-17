namespace Benzene.Core.Versioning.Casters;

public class CompositeCaster<TFrom, TIntermediate, TTo>(
    ICaster<TFrom, TIntermediate> caster1,
    ICaster<TIntermediate, TTo> caster2)
    : ICaster<TFrom, TTo>
{
    public TTo Cast(TFrom from)
    {
        return caster2.Cast(caster1.Cast(from));
    }
}

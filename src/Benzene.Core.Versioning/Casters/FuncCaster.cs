namespace Benzene.Core.Versioning.Casters;

public class FuncCaster<TFrom, TTo>(Func<TFrom, TTo> castFunc) : ICaster<TFrom, TTo>
{
    public TTo Cast(TFrom from) => castFunc(from);
}

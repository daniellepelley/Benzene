namespace Benzene.Core.Versioning.Casters;

public interface ICaster<TFrom, TTo>
{
    TTo Cast(TFrom from);
}

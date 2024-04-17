namespace Benzene.Core.Filters;

public interface IFilter<T>
{
    bool Filter(T value);
}

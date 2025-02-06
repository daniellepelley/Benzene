namespace Benzene.Core.MessageHandlers.Filters;

public interface IFilter<T>
{
    bool Filter(T value);
}

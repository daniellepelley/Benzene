namespace Benzene.Cache.Core;

public interface ICacheService
{
    Task<bool> CanConnectAsync();
}

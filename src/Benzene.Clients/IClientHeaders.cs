namespace Benzene.Clients
{
    public interface IClientHeaders
    {
        void Set(string key, string value);
        IDictionary<string, string> Get();
    }
}

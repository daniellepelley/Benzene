namespace Benzene.Clients
{
    public class ClientHeaders : IClientHeaders
    {
        private readonly IDictionary<string, string> _dictionary = new Dictionary<string, string>();

        public void Set(string key, string value)
        {
            _dictionary[key] = value;
        }

        public IDictionary<string, string> Get()
        {
            return _dictionary;
        }
    }
}

namespace Benzene.Clients
{
    /// <summary>
    /// Superseded by <see cref="OutboundContext"/>.Headers / <see cref="IBenzeneMessageSender"/>'s
    /// per-call <c>headers</c> parameter. See <c>work/benzene-clients-redesign-plan.md</c>.
    /// </summary>
    [Obsolete("Use OutboundContext.Headers / IBenzeneMessageSender.SendAsync's headers parameter instead - see work/benzene-clients-redesign-plan.md")]
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

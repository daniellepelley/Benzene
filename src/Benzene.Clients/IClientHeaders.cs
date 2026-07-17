namespace Benzene.Clients
{
    /// <summary>
    /// Superseded by <see cref="OutboundContext"/>.Headers / <see cref="IBenzeneMessageSender"/>'s
    /// per-call <c>headers</c> parameter. See <c>work/benzene-clients-redesign-plan.md</c>.
    /// </summary>
    [Obsolete("Use OutboundContext.Headers / IBenzeneMessageSender.SendAsync's headers parameter instead - see work/benzene-clients-redesign-plan.md")]
    public interface IClientHeaders
    {
        void Set(string key, string value);
        IDictionary<string, string> Get();
    }
}

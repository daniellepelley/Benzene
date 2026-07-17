namespace Benzene.Clients;

/// <summary>
/// Superseded by <see cref="IBenzeneMessageSender"/>, which routes by topic (the same key every
/// other Benzene transport already routes on) instead of by request type. See
/// <c>work/benzene-clients-redesign-plan.md</c>.
/// </summary>
[Obsolete("Use IBenzeneMessageSender instead - see work/benzene-clients-redesign-plan.md")]
public interface IClientMessageRouter
{
    IBenzeneMessageClient GetClient<TRequest>();
}
namespace Benzene.Clients;

/// <summary>
/// Superseded by <see cref="IBenzeneMessageSender"/> - a single topic-keyed
/// <c>SendAsync(topic, request)</c> call replaces resolving a client by service name/topic first.
/// See <c>work/benzene-clients-redesign-plan.md</c> §2.1.
/// </summary>
[Obsolete("Use IBenzeneMessageSender instead - see work/benzene-clients-redesign-plan.md")]
public interface IBenzeneMessageClientFactory
{
    IBenzeneMessageClient Create();
    IBenzeneMessageClient Create(string service, string topic);
}

namespace Benzene.Clients;

public interface IBenzeneMessageClientFactory
{
    IBenzeneMessageClient Create();
    IBenzeneMessageClient Create(string service, string topic);
}

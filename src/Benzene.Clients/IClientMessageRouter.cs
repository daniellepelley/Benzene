namespace Benzene.Clients;

public interface IClientMessageRouter
{
    IBenzeneMessageClient GetClient<TRequest>();
}
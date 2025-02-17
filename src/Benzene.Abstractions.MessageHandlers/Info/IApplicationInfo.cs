namespace Benzene.Abstractions.MessageHandlers.Info
{
    public interface IApplicationInfo
    {
        string Name { get; }
        string Description { get; }
        string Version { get; }
    }
}

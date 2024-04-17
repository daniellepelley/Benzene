namespace Benzene.Abstractions.Info
{
    public interface IApplicationInfo
    {
        string Name { get; }
        string Description { get; }
        string Version { get; }
    }
}

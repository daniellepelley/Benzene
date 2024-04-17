namespace Benzene.Diagnostics.Timers;

public interface IProcessTimer : IDisposable
{
    public void SetTag(string key, string value);
}

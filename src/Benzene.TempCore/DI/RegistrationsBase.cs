using System.Reflection;
using Benzene.Abstractions.DI;

namespace Benzene.Core.DI;

public abstract class RegistrationsBase : IRegistrations
{
    private readonly Dictionary<string, Func<IBenzeneServiceContainer, IBenzeneServiceContainer>> _dictionary = new();
    public string PackageName => Assembly.GetAssembly(GetType())!.FullName;

    public IDictionary<string, Type[]> GetRegistrations()
    {
        return _dictionary
            .ToDictionary(x => x.Key, x => GetTypes(x.Value));
    }

    protected void Add(string key, Func<IBenzeneServiceContainer, IBenzeneServiceContainer> func)
    {
        _dictionary.Add(key, func);
    }

    private static Type[] GetTypes(Func<IBenzeneServiceContainer, IBenzeneServiceContainer> func)
    {
        var recorder1 = new RegistrationRecorder();
        func(recorder1);
        return recorder1.GetTypes();
    }
}

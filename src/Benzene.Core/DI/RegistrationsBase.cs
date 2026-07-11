using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Benzene.Abstractions.DI;

namespace Benzene.Core.DI;

/// <summary>
/// Provides a base class for implementing registration modules that group related dependency injection registrations.
/// </summary>
public abstract class RegistrationsBase : IRegistrations
{
    private readonly Dictionary<string, Func<IBenzeneServiceContainer, IBenzeneServiceContainer>> _dictionary = new();

    /// <summary>
    /// Gets the package name from the assembly containing the registration implementation.
    /// </summary>
    public string PackageName => Assembly.GetAssembly(GetType())!.FullName;

    /// <summary>
    /// Gets the collection of registrations grouped by registration method name.
    /// </summary>
    /// <returns>A dictionary mapping registration method names to the types they register.</returns>
    public IDictionary<string, Type[]> GetRegistrations()
    {
        return _dictionary
            .ToDictionary(x => x.Key, x => GetTypes(x.Value));
    }

    /// <summary>
    /// Adds a registration method to the module.
    /// </summary>
    /// <param name="key">The name of the registration method.</param>
    /// <param name="func">The function that performs the registrations.</param>
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

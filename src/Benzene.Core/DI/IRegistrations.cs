using System;
using System.Collections.Generic;

namespace Benzene.Core.DI;

/// <summary>
/// Defines a registration module that groups related dependency injection registrations for validation and diagnostics.
/// </summary>
public interface IRegistrations
{
    /// <summary>
    /// Gets the package name that contains these registrations.
    /// </summary>
    string PackageName { get; }

    /// <summary>
    /// Gets the collection of registrations grouped by registration method name.
    /// </summary>
    /// <returns>A dictionary mapping registration method names to the types they register.</returns>
    IDictionary<string, Type[]> GetRegistrations();
}
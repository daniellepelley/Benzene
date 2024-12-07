using System;
using System.Collections.Generic;

namespace Benzene.Core.DI;

public interface IRegistrations
{
    string PackageName { get; }
    IDictionary<string, Type[]> GetRegistrations();
}
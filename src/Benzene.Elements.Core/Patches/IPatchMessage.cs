using System.Collections.Generic;

namespace Benzene.Elements.Core.Patches;

public interface IPatchMessage
{
    IList<string> UpdatedFields { get; }
}

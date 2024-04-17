using System.Collections.Generic;

namespace Benzene.Elements.Core.Patches;

public class PatchMessage : IPatchMessage
{
    public IList<string> UpdatedFields { get; } = new List<string>();
}

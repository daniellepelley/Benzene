using System.Collections.Generic;

namespace Benzene.Test.Autogen.CodeGen.Model;

public class HasLongDictionary
{
    // A non-string map: the generated property is Dictionary<string, long>, which still needs
    // `using System.Collections.Generic;` in the emitted file (regression guard for the
    // string-only using-statement bug).
    public Dictionary<string, long> Counters { get; set; }
}

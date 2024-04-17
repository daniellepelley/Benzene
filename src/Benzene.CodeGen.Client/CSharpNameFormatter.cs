using Benzene.CodeGen.Core;

namespace Benzene.CodeGen.Client;

public class CSharpNameFormatter : INameFormatter
{
    public string Format(string name)
    {
        return new FormatString(name)
            .EnsureStartsWithLetterOrUnderScore()
            .RemoveSpaces()
            .Pascalcase()
            .ToString();
    }
}

namespace Benzene.CodeGen.Core;

public class CodeFile : ICodeFile
{
    public CodeFile(string name, string[] lines)
    {
        Name = name;
        Lines = lines;
    }

    public string Name { get; set; }
    public string[] Lines { get; set; }
}
namespace Benzene.CodeGen.Core;

public interface ICodeFile
{
    public string Name { get; set; }
    public string[] Lines { get; set; }
}
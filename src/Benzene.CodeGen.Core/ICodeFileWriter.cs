namespace Benzene.CodeGen.Core;

public interface ICodeFileWriter
{
    Task CreateAsync(ICodeFile[] codeFiles, string directoryPath);
}

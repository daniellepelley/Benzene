namespace Benzene.CodeGen.Core;

public class CodeFileWriter : ICodeFileWriter
{
    public Task CreateAsync(ICodeFile[] codeFiles, string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        return Task.WhenAll(codeFiles.Select(codeFile => 
            File.WriteAllLinesAsync(Path.Combine(directoryPath, codeFile.Name), codeFile.Lines)));
    }
}

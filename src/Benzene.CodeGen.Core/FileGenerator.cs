namespace Benzene.CodeGen.Core;

public class FileGenerator<T>
{
    private readonly FileWriter _fileWriter = new();
    private readonly ICodeBuilder<T> _codeBuilder;

    public FileGenerator(ICodeBuilder<T> codeBuilder)
    {
        _codeBuilder = codeBuilder;
    }

    public async Task Generate(T input, string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var output = _codeBuilder.BuildCodeFiles(input);
        await _fileWriter.CreateAsync(output.ToFilesDictionary(), directoryPath);
    }
}
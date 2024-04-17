namespace Benzene.CodeGen.Core;

public interface IFileWriter
{
    Task CreateAsync(IDictionary<string, string[]> filesDictionary, string directoryPath);
}

namespace Benzene.CodeGen.Core;

public class FileWriter : IFileWriter
{
    public async Task CreateAsync(IDictionary<string, string> filesDictionary, string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        foreach (var file in filesDictionary)
        {
            await File.WriteAllTextAsync(Path.Combine(directoryPath, file.Key), file.Value);
        }
    }

    public async Task CreateAsync(IDictionary<string, string[]> filesDictionary, string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        foreach (var file in filesDictionary)
        {
            await File.WriteAllLinesAsync(Path.Combine(directoryPath, file.Key), file.Value);
        }
    }
}
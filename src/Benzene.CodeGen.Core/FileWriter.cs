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
            var path = Path.Combine(directoryPath, file.Key);
            EnsureParentDirectory(path);
            await File.WriteAllTextAsync(path, file.Value);
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
            var path = Path.Combine(directoryPath, file.Key);
            EnsureParentDirectory(path);
            await File.WriteAllLinesAsync(path, file.Value);
        }
    }

    // A file key may contain a sub-path (e.g. an atomic client's "UserGet/UserDto.cs"); File.Write*
    // won't create intermediate directories, so create the target file's parent first.
    private static void EnsureParentDirectory(string filePath)
    {
        var parent = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
        {
            Directory.CreateDirectory(parent);
        }
    }
}
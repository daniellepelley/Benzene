namespace Benzene.CodeGen.Core;

public interface IDirectoryMerger
{
    IDictionary<string, string[]> Merge(string directoryPath, IDictionary<string, string[]> newContent);
}
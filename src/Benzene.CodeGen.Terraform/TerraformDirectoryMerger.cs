using Benzene.CodeGen.Core;

namespace Benzene.CodeGen.Terraform;

public class TerraformDirectoryMerger : IDirectoryMerger
{
    public IDictionary<string, string[]> Merge(string directoryPath, IDictionary<string, string[]> newContent)
    {
        var output = new Dictionary<string, string[]>();
        foreach (var pair in newContent)
        {
            var filePath = $"{directoryPath}\\{pair.Key}";
            var exists = File.Exists(filePath);

            if (exists)
            {
                var input = File.ReadAllLines(filePath);

                var mergedContent = new DocumentMerger(
                        s => s.StartsWith(pair.Value.First()),
                        s => s.StartsWith("resource"))
                    .Merge(input, pair.Value);
                output.Add(pair.Key, mergedContent);
            }
            else
            {
                output.Add(pair.Key, pair.Value);
            }
        }

        return output;
    }
}

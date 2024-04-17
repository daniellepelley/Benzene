namespace Benzene.CodeGen.Terraform;

public class TerraformLambdaSettings
{
    public string Name { get; set; }
    public string EntryPoint { get; set; }
    public int Timeout { get; set; } = 30;
    public int MemorySize { get; set; } = 512;
    public int ReservedConcurrentExecutions = 3;
    public string Runtime { get; set; } = "dotnet6";
    public string Domain { get; set; }
    public string SubDomain { get; set; }
    public IDictionary<string, string[]> TopicsMap { get; set; }
}

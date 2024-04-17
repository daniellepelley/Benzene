namespace Benzene.CodeGen.Terraform;

public class TerraformLambdaEventBusPermissionsSettings
{
    public string LambdaName { get; set; }
    public IDictionary<string, string[]> TopicsMap { get; set; } 
}

using System.IO;
using Benzene.CodeGen.Terraform;
using Benzene.Test.Autogen.CodeGen.Helpers;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Terraform;

public class TerraformLambdaBuilderTest
{
    private string LoadExpected(string fileName) => File.ReadAllText($"{Directory.GetCurrentDirectory()}/Autogen/CodeGen/Terraform/Examples/{fileName}");

    [Fact]
    public void PedalCoreService_Test()
    {
        var expectedLambda = LoadExpected("PedalCore/lambda.txt");
        var expectedRole = LoadExpected("PedalCore/iam_roles.txt");

        var terraformBuilder = new TerraformLambdaBuilder();

        var result = terraformBuilder.Build(new TerraformLambdaSettings
        {
            Name = "platform-pedal-core-func",
            EntryPoint = "Platform.Pedal.Core.Func::Platform.Pedal.Core.LambdaEntryPoint::FunctionHandler",
            Timeout = 30,
            MemorySize = 2048,
            Domain = "platform",
            SubDomain = "pedal"
        });

        Assert.Equal(expectedLambda, result["lambda.tf"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedRole, result["iam_roles.tf"], ignoreLineEndingDifferences: true);
    }
}

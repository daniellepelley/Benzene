using System.IO;
using Benzene.CodeGen.Terraform;
using Benzene.Test.Autogen.CodeGen.Helpers;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Terraform;

public class TerraformLambdaBuilderTest
{
    private string LoadExpected(string fileName) => File.ReadAllText($"{Directory.GetCurrentDirectory()}/Autogen/CodeGen/Terraform/Examples/{fileName}");

    [Fact]
    public void MainCoreService_Test()
    {
        var expectedLambda = LoadExpected("MainCore/lambda.txt");
        var expectedRole = LoadExpected("MainCore/iam_roles.txt");

        var terraformBuilder = new TerraformLambdaBuilder();

        var result = terraformBuilder.Build(new TerraformLambdaSettings
        {
            Name = "benzene_main_core_func",
            EntryPoint = "Benzene.Main.Core.Func::benzene.main.Core.LambdaEntryPoint::FunctionHandlerAsync",
            Timeout = 30,
            MemorySize = 2048,
            Domain = "benzene",
            SubDomain = "main"
        });

        Assert.Equal(expectedLambda, result["lambda.tf"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedRole, result["iam_roles.tf"], ignoreLineEndingDifferences: true);
    }
}

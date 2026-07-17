using System;
using System.Collections.Generic;
using System.IO;
using Benzene.CodeGen.Terraform;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Terraform;

public class TerraformDirectoryMergerTest : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "benzene-terraform-merger-test-" + Guid.NewGuid());

    [Fact]
    public void Merge_FileDoesNotExist_ReturnsTheNewContentUnchanged()
    {
        Directory.CreateDirectory(_directory);
        var newContent = new Dictionary<string, string[]>
        {
            ["lambda.tf"] = new[] { "resource \"aws_lambda_function\" \"orders\" {", "}" }
        };

        var result = new TerraformDirectoryMerger().Merge(_directory, newContent);

        Assert.Equal(newContent["lambda.tf"], result["lambda.tf"]);
    }

    [Fact]
    public void Merge_DirectoryDoesNotExistYet_StillReturnsTheNewContentUnchanged()
    {
        var newContent = new Dictionary<string, string[]>
        {
            ["lambda.tf"] = new[] { "resource \"aws_lambda_function\" \"orders\" {", "}" }
        };

        var result = new TerraformDirectoryMerger().Merge(_directory, newContent);

        Assert.Equal(newContent["lambda.tf"], result["lambda.tf"]);
    }

    [Fact]
    public void Merge_ExistingFile_ReplacesTheMatchingResourceBlock()
    {
        Directory.CreateDirectory(_directory);
        var filePath = Path.Combine(_directory, "lambda.tf");
        File.WriteAllLines(filePath, new[]
        {
            "resource \"aws_lambda_function\" \"orders\" {",
            "  old_setting = \"old\"",
            "}"
        });

        var newContent = new Dictionary<string, string[]>
        {
            ["lambda.tf"] = new[]
            {
                "resource \"aws_lambda_function\" \"orders\" {",
                "  new_setting = \"new\"",
                "}"
            }
        };

        var result = new TerraformDirectoryMerger().Merge(_directory, newContent);

        Assert.Contains("  new_setting = \"new\"", result["lambda.tf"]);
        Assert.DoesNotContain("  old_setting = \"old\"", result["lambda.tf"]);
    }

    [Fact]
    public void Merge_ExistingFile_NoMatchingResource_AppendsTheNewContent()
    {
        Directory.CreateDirectory(_directory);
        var filePath = Path.Combine(_directory, "lambda.tf");
        File.WriteAllLines(filePath, new[]
        {
            "resource \"aws_lambda_function\" \"orders\" {",
            "  old_setting = \"old\"",
            "}"
        });

        var newContent = new Dictionary<string, string[]>
        {
            ["lambda.tf"] = new[]
            {
                "resource \"aws_iam_role\" \"orders\" {",
                "  name = \"orders\"",
                "}"
            }
        };

        var result = new TerraformDirectoryMerger().Merge(_directory, newContent);

        Assert.Contains("resource \"aws_lambda_function\" \"orders\" {", result["lambda.tf"]);
        Assert.Contains("resource \"aws_iam_role\" \"orders\" {", result["lambda.tf"]);
    }

    [Fact]
    public void Merge_MultipleFiles_EachMergedIndependently()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllLines(Path.Combine(_directory, "lambda.tf"), new[] { "resource \"aws_lambda_function\" \"orders\" {", "}" });

        var newContent = new Dictionary<string, string[]>
        {
            ["lambda.tf"] = new[] { "resource \"aws_lambda_function\" \"orders\" {", "  updated = true", "}" },
            ["iam_roles.tf"] = new[] { "resource \"aws_iam_role\" \"orders\" {", "}" }
        };

        var result = new TerraformDirectoryMerger().Merge(_directory, newContent);

        Assert.Equal(2, result.Count);
        Assert.Contains("  updated = true", result["lambda.tf"]);
        Assert.Equal(newContent["iam_roles.tf"], result["iam_roles.tf"]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}

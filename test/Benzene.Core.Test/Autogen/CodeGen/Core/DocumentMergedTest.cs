using System.IO;
using Benzene.CodeGen.Core;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Core;

public class DocumentMergedTest
{
    private string[] LoadLines(string fileName) => File.ReadAllLines($"Autogen/CodeGen/Core/Examples/{fileName}.tf");

    [Fact]
    public void Merge_Test()
    {
        var input = LoadLines("document1");
        var expected = LoadLines("document1-expected1");
        var newContent = LoadLines("document1-new-content1");

        var documentMerger = new DocumentMerger(
            s => s.StartsWith("resource \"aws_lambda_function\" \"function2\" {"),
            s => s.StartsWith("resource"));

        var actual = documentMerger.Merge(input, newContent);
        Assert.Equal(expected, actual);
    }
        
    [Fact]
    public void Merge_Test_EndOfFile()
    {
        var input = LoadLines("document1");
        var expected = LoadLines("document1-expected2");
        var newContent = LoadLines("document1-new-content2");

        var documentMerger = new DocumentMerger(
            s => s.StartsWith("resource \"aws_lambda_function\" \"function3\" {"),
            s => s.StartsWith("resource"));

        var actual = documentMerger.Merge(input, newContent);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Merge_Test_Added()
    {
        var input = LoadLines("document1");
        var expected = LoadLines("document1-expected3");
        var newContent = LoadLines("document1-new-content3");

        var documentMerger = new DocumentMerger(
            s => s.StartsWith("resource \"aws_lambda_function\" \"function4\" {"),
            s => s.StartsWith("resource"));

        var actual = documentMerger.Merge(input, newContent);

        Assert.Equal(expected, actual);
    }
}

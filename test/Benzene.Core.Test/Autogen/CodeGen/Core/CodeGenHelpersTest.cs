using Benzene.CodeGen.Core;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Core;

public class CodeGenHelpersTest
{
    [Theory]
    [InlineData("HelloWorld", "helloWorld")]
    [InlineData("ID", "id")]
    [InlineData("IDValue", "idvalue")]
    [InlineData("already", "already")]
    [InlineData("", "")]
    public void Camelcase_LowercasesLeadingUppercaseRun(string input, string expected)
    {
        Assert.Equal(expected, input.Camelcase().ToString());
    }

    [Theory]
    [InlineData("helloWorld", "HelloWorld")]
    [InlineData("a", "A")]
    [InlineData("", "")]
    public void Pascalcase_UppercasesFirstCharacter(string input, string expected)
    {
        Assert.Equal(expected, new FormatString(input).Pascalcase().ToString());
    }

    [Theory]
    [InlineData("1abc", "_1abc")]
    [InlineData("_abc", "_abc")]
    [InlineData("abc", "abc")]
    [InlineData("", "")]
    public void EnsureStartsWithLetterOrUnderScore_PrefixesUnderscoreWhenNeeded(string input, string expected)
    {
        Assert.Equal(expected, new FormatString(input).EnsureStartsWithLetterOrUnderScore().ToString());
    }

    [Theory]
    [InlineData("a b c", "abc")]
    [InlineData("nospaces", "nospaces")]
    [InlineData("", "")]
    public void RemoveSpaces_StripsAllSpaces(string input, string expected)
    {
        Assert.Equal(expected, new FormatString(input).RemoveSpaces().ToString());
    }

    [Theory]
    [InlineData("a-b_c!d", "ab_cd")]
    [InlineData("already_valid1", "already_valid1")]
    [InlineData("", "")]
    public void RemoveNonIdentifierCharacters_KeepsOnlyLettersDigitsAndUnderscores(string input, string expected)
    {
        Assert.Equal(expected, new FormatString(input).RemoveNonIdentifierCharacters().ToString());
    }

    [Fact]
    public void GenerateHash_SameInput_ProducesTheSameHash()
    {
        var first = CodeGenHelpers.GenerateHash("{\"a\":1}");
        var second = CodeGenHelpers.GenerateHash("{\"a\":1}");

        Assert.Equal(first, second);
    }

    [Fact]
    public void GenerateHash_DifferentInput_ProducesADifferentHash()
    {
        var first = CodeGenHelpers.GenerateHash("{\"a\":1}");
        var second = CodeGenHelpers.GenerateHash("{\"a\":2}");

        Assert.NotEqual(first, second);
    }
}

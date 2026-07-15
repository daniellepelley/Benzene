using Benzene.CodeGen.Core.Writers;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Core;

public class LineWriterTest
{
    [Fact]
    public void WriteLine_NoIndent_WritesTheLineAsIs()
    {
        var writer = new LineWriter();

        writer.WriteLine("public class Foo");

        Assert.Equal(new[] { "public class Foo" }, writer.GetLines());
    }

    [Fact]
    public void WriteLine_InsideStartIndent_PrefixesWithIndentSpaces()
    {
        var writer = new LineWriter(indentSize: 4);

        writer.WriteLine("class Foo");
        using (writer.StartIndent())
        {
            writer.WriteLine("public void Bar() { }");
        }
        writer.WriteLine("// back to top level");

        Assert.Equal(new[]
        {
            "class Foo",
            "    public void Bar() { }",
            "// back to top level"
        }, writer.GetLines());
    }

    [Fact]
    public void WriteLine_NestedStartIndents_StackTheIndentLevel()
    {
        var writer = new LineWriter(indentSize: 2);

        using (writer.StartIndent())
        {
            writer.WriteLine("level1");
            using (writer.StartIndent())
            {
                writer.WriteLine("level2");
            }
            writer.WriteLine("back to level1");
        }

        Assert.Equal(new[]
        {
            "  level1",
            "    level2",
            "  back to level1"
        }, writer.GetLines());
    }

    [Fact]
    public void WriteLine_ExplicitIndentCount_IgnoresCurrentIndentLevel()
    {
        var writer = new LineWriter(indentSize: 2);

        using (writer.StartIndent())
        {
            writer.WriteLine("explicit", indents: 3);
        }

        Assert.Equal(new[] { "      explicit" }, writer.GetLines());
    }

    [Fact]
    public void WriteLine_NoArguments_WritesABlankLine()
    {
        var writer = new LineWriter();

        writer.WriteLine();

        Assert.Equal(new[] { string.Empty }, writer.GetLines());
    }

    [Fact]
    public void WriteLines_Extension_WritesEachLineAtTheCurrentIndent()
    {
        var writer = new LineWriter(indentSize: 2);

        using (writer.StartIndent())
        {
            writer.WriteLines(new[] { "one", "two" });
        }

        Assert.Equal(new[] { "  one", "  two" }, writer.GetLines());
    }
}

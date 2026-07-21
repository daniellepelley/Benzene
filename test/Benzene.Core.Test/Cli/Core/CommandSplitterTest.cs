using System.Linq;
using Benzene.CodeGen.Cli.Core.Parsing;
using Xunit;

namespace Benzene.Test.Cli.Core
{
    public class CommandSplitterTest
    {
        [Fact]
        public void CommandSplitterTest1()
        {
            var input = "command value -attr1 value1 -attr2 value2";

            var output = new CommandSplitter().Split(input);

            Assert.Equal("command", output.ElementAt(0));
            Assert.Equal("value", output.ElementAt(1));
            Assert.Equal("-attr1", output.ElementAt(2));
            Assert.Equal("value1", output.ElementAt(3));
            Assert.Equal("-attr2", output.ElementAt(4));
            Assert.Equal("value2", output.ElementAt(5));
        }

        [Fact]
        public void CommandSplitterTest2()
        {
            var input = "command \"value\" -attr1 \"value1\" -attr2 \"value2\"";

            var output = new CommandSplitter().Split(input);

            Assert.Equal("command", output.ElementAt(0));
            Assert.Equal("value", output.ElementAt(1));
            Assert.Equal("-attr1", output.ElementAt(2));
            Assert.Equal("value1", output.ElementAt(3));
            Assert.Equal("-attr2", output.ElementAt(4));
            Assert.Equal("value2", output.ElementAt(5));
        }

        [Fact]
        public void CommandSplitterTest3()
        {
            var input = "command \"value one\" -attr1 \"value one\" -attr2 value2";

            var output = new CommandSplitter().Split(input);

            Assert.Equal("command", output.ElementAt(0));
            Assert.Equal("value one", output.ElementAt(1));
            Assert.Equal("-attr1", output.ElementAt(2));
            Assert.Equal("value one", output.ElementAt(3));
            Assert.Equal("-attr2", output.ElementAt(4));
            Assert.Equal("value2", output.ElementAt(5));
        }

        [Fact]
        public void Split_UnterminatedQuote_DoesNotThrowAndTreatsRestAsQuotedContent()
        {
            // A missing closing quote must not crash the parser (was an IndexOutOfRangeException
            // when the inner loop ran past the end of the string looking for the closing quote).
            var input = "command -name \"value one";

            var output = new CommandSplitter().Split(input);

            Assert.Equal("command", output.ElementAt(0));
            Assert.Equal("-name", output.ElementAt(1));
            Assert.Equal("value one", output.ElementAt(2));
        }

        [Fact]
        public void Split_QuotedFinalArgument_DoesNotEmitTrailingEmptyToken()
        {
            var input = "command -name \"value\"";

            var output = new CommandSplitter().Split(input);

            Assert.Equal(new[] { "command", "-name", "value" }, output);
        }

        [Fact]
        public void Split_TrailingSpace_DoesNotEmitTrailingEmptyToken()
        {
            var input = "command value ";

            var output = new CommandSplitter().Split(input);

            Assert.Equal(new[] { "command", "value" }, output);
        }
    }
}

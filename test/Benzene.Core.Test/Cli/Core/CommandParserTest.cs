using System.Linq;
using Benzene.CodeGen.Cli.Core.Parsing;
using Xunit;

namespace Benzene.Test.Cli.Core
{
    public class CommandParserTest
    {
        [Fact]
        public void CommandParserTest1()
        {
            var input = "command value -attr1 \"value1\" -attr2 value2";

            var output = new CommandParser().Parse(input);

            Assert.Equal("command", output.Name);
            Assert.Equal("value", output.Value);
            Assert.Equal("attr1", output.Attributes.ElementAt(0).Key);
            Assert.Equal("value1", output.Attributes.ElementAt(0).Value);
            Assert.Equal("attr2", output.Attributes.ElementAt(1).Key);
            Assert.Equal("value2", output.Attributes.ElementAt(1).Value);
        }

        [Fact]
        public void CommandParserTest2()
        {
            var input = "command \"value one\" -attr1 -attr2 value2";

            var output = new CommandParser().Parse(input);

            Assert.Equal("command", output.Name);
            Assert.Equal("value one", output.Value);
            Assert.Equal("attr1", output.Attributes.ElementAt(0).Key);
            Assert.Null(output.Attributes.ElementAt(0).Value);
            Assert.Equal("attr2", output.Attributes.ElementAt(1).Key);
            Assert.Equal("value2", output.Attributes.ElementAt(1).Value);
        }

        [Fact]
        public void CommandParserTest3()
        {
            var input = "command -attr1 value1 -attr2";

            var output = new CommandParser().Parse(input);

            Assert.Equal("command", output.Name);
            Assert.Null(output.Value);
            Assert.Equal("attr1", output.Attributes.ElementAt(0).Key);
            Assert.Equal("value1", output.Attributes.ElementAt(0).Value);
            Assert.Equal("attr2", output.Attributes.ElementAt(1).Key);
            Assert.Null(output.Attributes.ElementAt(1).Value);
        }
    }
}

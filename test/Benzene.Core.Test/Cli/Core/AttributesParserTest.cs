using System.Linq;
using Benzene.CodeGen.Cli.Core.Parsing;
using Xunit;

namespace Benzene.Test.Cli.Core
{
    public class AttributesParserTest
    {
        [Fact]
        public void AttributesParserTest1()
        {
            var input = "-attr1 value1 -attr2 value2";

            var output = new AttributesParser().Parse(input.Split(' '));

            Assert.Equal("attr1", output.ElementAt(0).Key);
            Assert.Equal("value1", output.ElementAt(0).Value);
            Assert.Equal("attr2", output.ElementAt(1).Key);
            Assert.Equal("value2", output.ElementAt(1).Value);
        }

        [Fact]
        public void AttributesParserTest2()
        {
            var input = "command value -attr1 --attr2 value2";

            var output = new AttributesParser().Parse(input.Split(' '));

            Assert.Equal("attr1", output.ElementAt(0).Key);
            Assert.Null(output.ElementAt(0).Value);
            Assert.Equal("attr2", output.ElementAt(1).Key);
            Assert.Equal("value2", output.ElementAt(1).Value);
        }

        [Fact]
        public void AttributesParserTest3()
        {
            var input = "-attr1 value1 -attr2 -attr3";

            var output = new AttributesParser().Parse(input.Split(' '));

            Assert.Equal("attr1", output.ElementAt(0).Key);
            Assert.Equal("value1", output.ElementAt(0).Value);
            Assert.Equal("attr2", output.ElementAt(1).Key);
            Assert.Null(output.ElementAt(1).Value);
            Assert.Equal("attr3", output.ElementAt(2).Key);
            Assert.Null(output.ElementAt(2).Value);
        }
    }
}

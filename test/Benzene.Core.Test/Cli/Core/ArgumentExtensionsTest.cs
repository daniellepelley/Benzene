using System.Collections.Generic;
using Benzene.CodeGen.Cli.Core.Parsing;
using Xunit;

namespace Benzene.Test.Cli.Core
{
    public class ArgumentExtensionsTest
    {
        [Fact]
        public void GetValue_KeyPresentWithValue_ReturnsValue()
        {
            var args = new CommandArguments
            {
                Attributes = new Dictionary<string, string?> { { "format", "yaml" } }
            };

            Assert.Equal("yaml", args.GetValue("format", "json"));
        }

        [Fact]
        public void GetValue_KeyMissing_ReturnsDefault()
        {
            var args = new CommandArguments { Attributes = new Dictionary<string, string?>() };

            Assert.Equal("json", args.GetValue("format", "json"));
        }

        [Fact]
        public void GetValue_KeyPresentWithNullValue_ReturnsDefault()
        {
            // A bare value-less flag ("--format") is stored as key -> null by AttributesParser.
            // GetValue must fall back to the configured default rather than returning null (its
            // return type is non-null string, and callers feed the result into string payload
            // properties).
            var args = new CommandArguments
            {
                Attributes = new Dictionary<string, string?> { { "format", null } }
            };

            Assert.Equal("json", args.GetValue("format", "json"));
        }
    }
}

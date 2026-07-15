using System.Collections.Generic;
using Benzene.CodeGen.Cli.Core.Commands.HealthCheck;
using Benzene.CodeGen.Cli.Core.Parsing;
using Xunit;

namespace Benzene.Test.Cli.Core
{
    public class PayloadMapperTest
    {
        [Fact]
        public void Map_AttributesPresent_SetsMatchingProperties()
        {
            var commandArguments = new CommandArguments
            {
                Name = "health-check",
                Attributes = new Dictionary<string, string?>
                {
                    { "profile", "my-profile" },
                    { "lambda-name", "my-lambda" }
                }
            };

            var payload = PayloadMapper.Map<HealthCheckPayload>(commandArguments);

            Assert.Equal("my-profile", payload.Profile);
            Assert.Equal("my-lambda", payload.LambdaName);
        }

        [Fact]
        public void Map_AttributeMissing_LeavesPropertyEmpty()
        {
            var commandArguments = new CommandArguments
            {
                Name = "health-check",
                Attributes = new Dictionary<string, string?>()
            };

            var payload = PayloadMapper.Map<HealthCheckPayload>(commandArguments);

            Assert.Equal(string.Empty, payload.Profile);
            Assert.Equal(string.Empty, payload.LambdaName);
        }
    }
}

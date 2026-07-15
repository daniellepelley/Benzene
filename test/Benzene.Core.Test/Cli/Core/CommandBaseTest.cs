using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.CodeGen.Cli.Core;
using Benzene.CodeGen.Cli.Core.Commands.HealthCheck;
using Benzene.CodeGen.Cli.Core.Parsing;
using Xunit;

namespace Benzene.Test.Cli.Core
{
    public class CommandBaseTest
    {
        // Captures the payload it was handed instead of doing real work (unlike HealthCheckCommand,
        // which calls out to a live AWS Lambda client), so CommandBase<TPayload>'s own glue - mapping
        // CommandArguments to TPayload and delegating GetHelp() to HelpGenerator - can be verified in
        // isolation.
        private class CapturingCommand : CommandBase<HealthCheckPayload>
        {
            public HealthCheckPayload ReceivedPayload { get; private set; }

            public CapturingCommand() : base("healthcheck", "Runs a health check on a Benzene service") { }

            public override Task ExecuteAsync(HealthCheckPayload commandPayload)
            {
                ReceivedPayload = commandPayload;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task ExecuteAsync_CommandArguments_MapsAttributesIntoThePayloadBeforeDelegating()
        {
            var command = new CapturingCommand();
            var args = new CommandArguments
            {
                Name = "healthcheck",
                Attributes = new Dictionary<string, string?>
                {
                    { "profile", "my-profile" },
                    { "lambda-name", "my-lambda" }
                }
            };

            await command.ExecuteAsync(args);

            Assert.Equal("my-profile", command.ReceivedPayload.Profile);
            Assert.Equal("my-lambda", command.ReceivedPayload.LambdaName);
        }

        [Fact]
        public void GetHelp_DelegatesToHelpGenerator_UsingNameAndDescription()
        {
            var command = new CapturingCommand();

            var help = command.GetHelp();

            Assert.Contains("healthcheck", help);
            Assert.Contains("Runs a health check on a Benzene service", help);
            Assert.Contains("--profile", help);
        }
    }
}

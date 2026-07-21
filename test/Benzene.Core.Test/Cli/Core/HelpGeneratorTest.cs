using Benzene.CodeGen.Cli.Core;
using Benzene.CodeGen.Cli.Core.Commands.HealthCheck;
using Benzene.CodeGen.Cli.Core.Parsing;
using Xunit;

namespace Benzene.Test.Cli.Core
{
    public class HelpGeneratorTest
    {
        [Fact]
        public void Generate_IncludesNameDescriptionAndEachArgAttribute()
        {
            var help = HelpGenerator.Generate<HealthCheckPayload>("health-check", "Checks the health of a lambda");

            Assert.Contains("health-check", help);
            Assert.Contains("Checks the health of a lambda", help);
            Assert.Contains("--profile", help);
            Assert.Contains(Constants.ProfileDescription, help);
            Assert.Contains("--lambda-name", help);
            Assert.Contains(Constants.LambdaNameDescription, help);
        }

        [Fact]
        public void Generate_IndentsNameAndDescription()
        {
            // The name/description were meant to be indented (like "  Parameters" and "    --…"), but
            // the spaces were placed inside the interpolation braces ($"{  name}") where they are
            // discarded, so both printed flush-left, misaligned with the rest of the output.
            var help = HelpGenerator.Generate<HealthCheckPayload>("health-check", "Checks the health of a lambda");

            Assert.Contains("  health-check", help);
            Assert.Contains("    Checks the health of a lambda", help);
        }
    }
}

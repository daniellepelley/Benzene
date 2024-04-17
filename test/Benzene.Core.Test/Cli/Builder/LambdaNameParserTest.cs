using Benzene.CodeGen.Cli.Core.Commands.Build;
using Xunit;

namespace Benzene.Test.Cli.Builder
{
    public class LambdaNameParserTest
    {
        [Theory]
        [InlineData("platform-pedal-core-func", "Client","Platform.Pedal.Core.Client")]
        [InlineData("platform-tenant-core-func","Client","Platform.Tenant.Core.Client")]
        [InlineData("platform-tenant-core-func","Func","Platform.Tenant.Core.Func")]
        public void GetNamespace(string input, string suffix, string expected)
        {
            var actual = LambdaNameParser.GetNamespace(input, suffix);
            Assert.Equal(expected, actual);
        }
        
        
        [Theory]
        [InlineData("platform-pedal-core-func","PlatformPedalCore")]
        [InlineData("platform-tenant-core-func","PlatformTenantCore")]
        public void GetServiceName(string input, string expected)
        {
            var actual = LambdaNameParser.GetServiceName(input);
            Assert.Equal(expected, actual);
        }
    }
}

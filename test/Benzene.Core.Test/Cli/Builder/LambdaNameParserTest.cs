using Benzene.CodeGen.Cli.Core.Commands.Build;
using Xunit;

namespace Benzene.Test.Cli.Builder
{
    public class LambdaNameParserTest
    {
        [Theory]
        [InlineData("benzene-main-core-func", "Client","Benzene.Main.Core.Client")]
        [InlineData("benzene-tenant-core-func","Client","Benzene.Tenant.Core.Client")]
        [InlineData("benzene-tenant-core-func","Func","Benzene.Tenant.Core.Func")]
        public void GetNamespace(string input, string suffix, string expected)
        {
            var actual = LambdaNameParser.GetNamespace(input, suffix);
            Assert.Equal(expected, actual);
        }
        
        
        [Theory]
        [InlineData("benzene-main-core-func","BenzeneMainCore")]
        [InlineData("benzene-tenant-core-func","BenzeneTenantCore")]
        public void GetServiceName(string input, string expected)
        {
            var actual = LambdaNameParser.GetServiceName(input);
            Assert.Equal(expected, actual);
        }
    }
}

using System.Linq;
using Benzene.HealthChecks;
using Xunit;

namespace Benzene.Test.Plugins.HealthChecks
{
    public class HealthCheckNamerTests
    {
        private const string HealthCheckName = "some-name";
        private readonly HealthCheckNamer _healthCheckNamer;
        
        public HealthCheckNamerTests()
        {
            _healthCheckNamer = new HealthCheckNamer();
        }
        
        [Fact]
        public void GetName_ReturnsPassedInName_WhenNameIsNotEmpty()
        {
            var result = _healthCheckNamer.GetName(HealthCheckName);
            Assert.Equal(HealthCheckName, result);
        }
        
        [Fact]
        public void GetName_ReturnsHealthCheckWithIndex_WhenNameIsEmpty()
        {
            var expectedNames = new[] { "HealthCheck-1", "HealthCheck-2", "HealthCheck-3" };
            var actualNames = new[]
            {
                _healthCheckNamer.GetName(string.Empty),
                _healthCheckNamer.GetName(string.Empty),
                _healthCheckNamer.GetName(string.Empty)
            };
            Assert.Equal(expectedNames, actualNames);
        }
        
        [Fact]
        public void GetName_AlwaysReturnsUniqueName_WhenDuplicatesArePassedIn()
        {
            var expectedNames = new[] { HealthCheckName, $"{HealthCheckName}-2", $"{HealthCheckName}-3" };
            var actualNames = new[]
            {
                _healthCheckNamer.GetName(HealthCheckName),
                _healthCheckNamer.GetName(HealthCheckName),
                _healthCheckNamer.GetName(HealthCheckName)
            };
            Assert.Equal(expectedNames, actualNames);
        }

        [Fact]
        public void GetName_ReturnsUniqueNames_WhenAGeneratedSuffixCollidesWithALiteralType()
        {
            // "a","a" produces "a" then the generated "a-2"; a third check literally typed "a-2" must
            // NOT re-produce "a-2" (which made HealthCheckProcessor's ToDictionary throw and 500 the
            // whole probe). Every returned name must be distinct.
            var names = new[]
            {
                _healthCheckNamer.GetName("a"),
                _healthCheckNamer.GetName("a"),
                _healthCheckNamer.GetName("a-2")
            };

            Assert.Equal(names.Length, names.Distinct().Count());
        }
    }
}

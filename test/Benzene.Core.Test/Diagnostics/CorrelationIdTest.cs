using Benzene.Diagnostics.Correlation;
using Xunit;

namespace Benzene.Test.Diagnostics;

public class CorrelationIdTest
{
    [Fact]
    public void Get_NothingSet_ReturnsANonEmptySelfGeneratedValue()
    {
        var correlationId = new CorrelationId();

        Assert.False(string.IsNullOrEmpty(correlationId.Get()));
    }

    [Fact]
    public void Set_ValidValue_OverridesTheSelfGeneratedValue()
    {
        var correlationId = new CorrelationId();

        correlationId.Set("my-correlation-id");

        Assert.Equal("my-correlation-id", correlationId.Get());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Set_NullOrEmptyValue_LeavesTheExistingValueUnchanged(string value)
    {
        var correlationId = new CorrelationId();
        var original = correlationId.Get();

        correlationId.Set(value);

        Assert.Equal(original, correlationId.Get());
    }

    [Fact]
    public void Set_CalledTwice_LatestValueWins()
    {
        var correlationId = new CorrelationId();

        correlationId.Set("first");
        correlationId.Set("second");

        Assert.Equal("second", correlationId.Get());
    }
}

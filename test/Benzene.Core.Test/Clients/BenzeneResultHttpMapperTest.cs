using Benzene.Clients.Common;
using Benzene.Results;
using Xunit;

namespace Benzene.Test.Clients;

public class BenzeneResultHttpMapperTest
{
    [Theory]
    [InlineData("408", BenzeneResultStatus.Timeout)]
    [InlineData("429", BenzeneResultStatus.TooManyRequests)]
    [InlineData("500", BenzeneResultStatus.UnexpectedError)]
    [InlineData("502", BenzeneResultStatus.ServiceUnavailable)]
    [InlineData("504", BenzeneResultStatus.Timeout)]
    public void Map_TransientAndServerErrorCodes_MapToTheirStatuses(string httpCode, string expectedStatus)
    {
        var result = BenzeneResultHttpMapper.Map<string>(httpCode);

        Assert.Equal(expectedStatus, result.Status);
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void Map_UnmappedCode_ReportsTheActualCodeInTheError()
    {
        var result = BenzeneResultHttpMapper.Map<string>("418");

        Assert.Equal(BenzeneResultStatus.UnexpectedError, result.Status);
        Assert.Contains(result.Errors, x => x.Contains("418"));
    }

    [Theory]
    [InlineData(BenzeneResultStatus.TooManyRequests)]
    [InlineData(BenzeneResultStatus.Timeout)]
    [InlineData(BenzeneResultStatus.Updated)]
    public void NormalizeStatus_PassesKnownBenzeneStatusesThroughVerbatim(string status)
    {
        Assert.Equal(status, BenzeneResultHttpMapper.NormalizeStatus(status));
    }

    [Theory]
    [InlineData("429", BenzeneResultStatus.TooManyRequests)]
    [InlineData("504", BenzeneResultStatus.Timeout)]
    [InlineData("502", BenzeneResultStatus.ServiceUnavailable)]
    public void NormalizeStatus_MapsNumericCodes(string httpCode, string expectedStatus)
    {
        Assert.Equal(expectedStatus, BenzeneResultHttpMapper.NormalizeStatus(httpCode));
    }

    [Fact]
    public void NormalizeStatus_UnrecognizedValues_ReturnNull()
    {
        Assert.Null(BenzeneResultHttpMapper.NormalizeStatus("418"));
        Assert.Null(BenzeneResultHttpMapper.NormalizeStatus("SomeCustomStatus"));
        Assert.Null(BenzeneResultHttpMapper.NormalizeStatus(null));
    }
}

using Benzene.Aws.Lambda.Kinesis;
using Xunit;

namespace Benzene.Test.Aws.Kinesis;

public class KinesisBatchResponseTest
{
    [Fact]
    public void Constructor_NoFailedSequenceNumber_BatchItemFailuresIsEmpty()
    {
        var response = new KinesisBatchResponse();

        Assert.Empty(response.BatchItemFailures);
    }

    [Fact]
    public void Constructor_FailedSequenceNumber_BatchItemFailuresHasOneEntry()
    {
        var response = new KinesisBatchResponse("3");

        var failure = Assert.Single(response.BatchItemFailures);
        Assert.Equal("3", failure.ItemIdentifier);
    }
}

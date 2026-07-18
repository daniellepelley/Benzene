using System.Threading.Tasks;
using Amazon.Lambda.S3Events;
using Benzene.Aws.S3;
using Benzene.Core.MessageHandlers;
using Benzene.Results;
using Xunit;

namespace Benzene.Test.Aws.S3;

public class S3MessageHandlerResultSetterTest
{
    private static S3RecordContext CreateContext()
    {
        var record = new S3Event.S3EventNotificationRecord { EventName = "ObjectCreated:Put" };
        return S3RecordContext.CreateInstance(new S3Event { Records = [record] }, record);
    }

    [Fact]
    public async Task SetResultAsync_SuccessfulResult_RecordsSuccessfulMessageResult()
    {
        var context = CreateContext();
        var setter = new S3MessageHandlerResultSetter();

        await setter.SetResultAsync(context, new MessageHandlerResult(BenzeneResult.Ok()));

        Assert.NotNull(context.MessageResult);
        Assert.True(context.MessageResult.IsSuccessful);
    }

    [Fact]
    public async Task SetResultAsync_FailedResult_RecordsUnsuccessfulMessageResult()
    {
        var context = CreateContext();
        var setter = new S3MessageHandlerResultSetter();

        await setter.SetResultAsync(context, new MessageHandlerResult(BenzeneResult.NotFound("not found")));

        Assert.NotNull(context.MessageResult);
        Assert.False(context.MessageResult.IsSuccessful);
    }
}

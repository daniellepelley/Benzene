using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Grpc;
using Benzene.Grpc.Test.Protos;
using Benzene.Results;

namespace Benzene.Grpc.Test.Handlers;

[GrpcMethod("/benzene.test.TestService/Upload")]
[Message("grpc-test-upload-stream-topic")]
public class UploadStreamMessageHandler : IMessageHandler<IAsyncEnumerable<UploadItem>, UploadSummary>
{
    public async Task<IBenzeneResult<UploadSummary>> HandleAsync(IAsyncEnumerable<UploadItem> request)
    {
        var total = 0;
        await foreach (var item in request)
        {
            total += item.Value;
        }

        return BenzeneResult.Ok(new UploadSummary { Total = total });
    }
}

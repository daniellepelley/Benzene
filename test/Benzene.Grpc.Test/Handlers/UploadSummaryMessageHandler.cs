using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Grpc.Test.Protos;
using Benzene.Results;

namespace Benzene.Grpc.Test.Handlers;

[Message("grpc-test-upload-topic")]
public class UploadSummaryMessageHandler : IMessageHandler<UploadItem, UploadSummary>
{
    public Task<IBenzeneResult<UploadSummary>> HandleAsync(UploadItem request)
    {
        return Task.FromResult(BenzeneResult.Ok(new UploadSummary { Total = request.Value * 2 }));
    }
}

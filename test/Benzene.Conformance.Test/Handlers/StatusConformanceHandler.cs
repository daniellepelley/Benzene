using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Results;

namespace Benzene.Conformance.Test.Handlers;

public class StatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string[]? Errors { get; set; }
}

public class StatusReply
{
    public string Applied { get; set; } = string.Empty;
}

/// <summary>
/// Canonical conformance handler (see docs/specification/conformance/README.md): returns the requested
/// status verbatim - success-class statuses carry the payload <c>{ "applied": "&lt;status&gt;" }</c>,
/// failure-class statuses carry the requested errors.
/// </summary>
[Message("conformance:status")]
public class StatusConformanceHandler : IMessageHandler<StatusRequest, StatusReply>
{
    public Task<IBenzeneResult<StatusReply>> HandleAsync(StatusRequest request)
    {
        var result = BenzeneResultStatus.IsSuccess(request.Status)
            ? BenzeneResult.Set(request.Status, new StatusReply { Applied = request.Status })
            : BenzeneResult.Set<StatusReply>(request.Status, request.Errors ?? Array.Empty<string>());

        return Task.FromResult(result);
    }
}

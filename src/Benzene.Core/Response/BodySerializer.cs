using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.Response;

public class BodySerializer<TContext> : IBodySerializer// where TContext : class, IHasMessageResult
{
    private readonly IResponsePayloadMapper<TContext> _responsePayloadMapper;
    private readonly TContext _context;

    public BodySerializer(IResponsePayloadMapper<TContext> responsePayloadMapper, TContext context)
    {
        _context = context;
        _responsePayloadMapper = responsePayloadMapper;
    }

    public string Serialize(ISerializer serializer)
    {
        return _responsePayloadMapper.Map(_context, serializer);
    }
}

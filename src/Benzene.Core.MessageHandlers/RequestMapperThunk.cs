using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Request;

namespace Benzene.Core.MessageHandlers;

public class RequestMapperThunk<TContext> : IRequestMapperThunk
{
    private readonly TContext _context;
    private readonly IRequestMapper<TContext> _requestMapper;

    public RequestMapperThunk(IRequestMapper<TContext> requestMapper, TContext context)
    {
        _context = context;
        _requestMapper = requestMapper;
    }

    public TRequest? GetRequest<TRequest>() where TRequest : class
    {
        return _requestMapper.GetBody<TRequest>(_context);
    }
}
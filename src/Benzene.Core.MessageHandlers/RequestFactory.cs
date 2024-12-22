using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Request;

namespace Benzene.Core.MessageHandlers;

public class RequestFactory<TContext> : IRequestFactory
{
    private readonly TContext _context;
    private readonly IRequestMapper<TContext> _messageMapper;

    public RequestFactory(IRequestMapper<TContext> messageMapper, TContext context)
    {
        _context = context;
        _messageMapper = messageMapper;
    }

    public TRequest? GetRequest<TRequest>() where TRequest : class
    {
        return _messageMapper.GetBody<TRequest>(_context);
    }
}
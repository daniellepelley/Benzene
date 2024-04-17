using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Results;

namespace Benzene.Core.MessageHandling;

public class RequestFactory<TContext> : IRequestFactory where TContext : IHasMessageResult
{
    private readonly TContext _context;
    private readonly IRequestMapper<TContext> _messageMapper;

    public RequestFactory(IRequestMapper<TContext> messageMapper, TContext context)
    {
        _context = context;
        _messageMapper = messageMapper;
    }

    public TRequest GetRequest<TRequest>() where TRequest : class
    {
        return _messageMapper.GetBody<TRequest>(_context);
    }
}
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;
using Benzene.Core.Serialization;

namespace Benzene.Core.Response;

public class JsonSerializationResponseHandler<TContext> : ISerializationResponseHandler<TContext> where TContext : class
{
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;

    public JsonSerializationResponseHandler(IBenzeneResponseAdapter<TContext> benzeneResponseAdapter)
    {
        _benzeneResponseAdapter = benzeneResponseAdapter;
    }

    public void HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult, IBodySerializer bodySerializer)
    {
        if (!string.IsNullOrEmpty(_benzeneResponseAdapter.GetBody(context)))
        {
            return;
        }

        _benzeneResponseAdapter.SetBody(context, bodySerializer.Serialize(new JsonSerializer(), messageHandlerResult));
        _benzeneResponseAdapter.SetContentType(context, "application/json");
    }
}

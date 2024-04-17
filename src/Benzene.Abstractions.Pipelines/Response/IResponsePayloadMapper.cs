using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;

namespace Benzene.Abstractions.Response;

/// <summary>
/// Converts a message result into a response body using the passed serializer 
/// </summary>
public interface IResponsePayloadMapper<TContext> where TContext : IHasMessageResult
{
    string Map(TContext context, ISerializer serializer);
}
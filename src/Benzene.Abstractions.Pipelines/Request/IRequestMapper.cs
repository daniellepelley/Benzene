namespace Benzene.Abstractions.Request;

public interface IRequestMapper<in TContext>
{
    TRequest GetBody<TRequest>(TContext context) where TRequest : class;
}

// public class JsonRequestMapper<TContext> : RequestMapper<TContext>
// {
//     public JsonRequestMapper(IMessageBodyMapper<TContext> messageBodyMapper)
//         :base(messageBodyMapper, new JsonSerializer())
//     { }
// }
using Benzene.Abstractions.MessageHandling;
using Benzene.Grpc;
using Benzene.Results;

namespace Benzene.Example.Grpc.Handlers;

[GrpcMethod("/greet.Greeter/SayHello")]
[Message("say_hello")]
public class SayHelloMessageHandler : IMessageHandler<HelloRequest, HelloReply>
{
    public Task<IServiceResult<HelloReply>> HandleAsync(HelloRequest request)
    {
        return ServiceResult.Ok(new HelloReply
        {
            Message = "Hello " + request.Name + ", this is Benzene"
        }).AsTask();
    }
}
public class HelloRequest2
{
    public string Name { get; set; }
}

public class HelloReply2
{
    public string Message { get; set; }
}
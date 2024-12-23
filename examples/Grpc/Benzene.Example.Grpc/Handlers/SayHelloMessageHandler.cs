﻿using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Grpc;
using Benzene.Results;

namespace Benzene.Example.Grpc.Handlers;

[GrpcMethod("/greet.Greeter/SayHello")]
[Message("say_hello")]
public class SayHelloMessageHandler : IMessageHandler<HelloRequest2, HelloReply2>
{
    public Task<IServiceResult<HelloReply2>> HandleAsync(HelloRequest2 request)
    {
        return ServiceResult.Ok(new HelloReply2
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
﻿using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Grpc;

public class GrpcMessageBodyGetter : IMessageBodyGetter<GrpcContext>
{
    public string? GetBody(GrpcContext context)
    {
        return System.Text.Json.JsonSerializer.Serialize(context.RequestAsObject);
    }
}
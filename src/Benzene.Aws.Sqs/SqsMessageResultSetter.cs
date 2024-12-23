﻿using System.Threading.Tasks;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Results;

namespace Benzene.Aws.Sqs;

public class SqsMessageResultSetter : IResultSetter<SqsMessageContext>
{
    public Task SetResultAsync(SqsMessageContext context, IMessageHandlerResult messageHandlerResult)
    {
        context.IsSuccessful = messageHandlerResult.BenzeneResult.IsSuccessful;
        return Task.CompletedTask;
    }
}
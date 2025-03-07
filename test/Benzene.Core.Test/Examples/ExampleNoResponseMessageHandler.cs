﻿using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;

namespace Benzene.Test.Examples;

[Message(Defaults.TopicNoResponse)]
[HttpEndpoint(Defaults.Method, Defaults.PathNoResponse)]
public class ExampleNoResponseMessageHandler : IMessageHandler<ExampleRequestPayload>
{
    public Task HandleAsync(ExampleRequestPayload request)
    {
        return Task.FromResult(BenzeneResult.Ok(new Void()));
    }
}

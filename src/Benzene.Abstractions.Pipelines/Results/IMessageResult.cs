﻿using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;

namespace Benzene.Abstractions.Results;

public interface IMessageResult
{
    ITopic Topic { get; }
    IMessageHandlerDefinition MessageHandlerDefinition { get; }
    string Status { get; }
    bool IsSuccessful { get; }
    object Payload { get; }
    string[] Errors { get; }
}
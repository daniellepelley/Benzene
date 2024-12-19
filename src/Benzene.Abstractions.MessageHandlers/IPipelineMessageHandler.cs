﻿using Benzene.Results;

namespace Benzene.Abstractions.MessageHandlers;

public interface IPipelineMessageHandler<TRequest, TResponse>
{
    Task<IServiceResult<TResponse>> HandleAsync(ITopic topic, TRequest request);
}
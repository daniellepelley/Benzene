﻿using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;

namespace Benzene.SelfHost;

public interface IBenzeneWorkerBuilder : IRegisterDependency
{
    void Add(Func<IServiceResolverFactory, IBenzeneWorker> func);
    IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();
    IBenzeneWorker Create(IServiceResolverFactory serviceResolverFactory);
}

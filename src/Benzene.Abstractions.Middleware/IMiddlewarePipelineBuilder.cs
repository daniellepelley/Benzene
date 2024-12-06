﻿using Benzene.Abstractions.DI;
using Benzene.Abstractions.MiddlewareBuilder;

namespace Benzene.Abstractions.Middleware;

public interface IMiddlewarePipelineBuilder<TContext> : IRegisterDependency
 {
     IMiddlewarePipelineBuilder<TContext> Use(Func<IServiceResolver, IMiddleware<TContext>> func);
     IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();
     IMiddlewarePipeline<TContext> Build();
 }

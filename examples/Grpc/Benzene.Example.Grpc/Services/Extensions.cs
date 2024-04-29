using Benzene.Abstractions.DI;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.AspNet.Core;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Grpc;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Mvc;
using ServiceDescriptor = Google.Protobuf.Reflection.ServiceDescriptor;

namespace Benzene.Example.Grpc.Services
{
    public static class Extensions
    {
    public static IAspApplicationBuilder UseGrpc(this IAspApplicationBuilder app, Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>> action)
    {
        // builder.Services.AddGrpc(x => x.Interceptors.Add(typeof(BenzeneInterceptor)));
// var benzeneServiceContainer = new MicrosoftBenzeneServiceContainer(builder.Services);
// builder.Services.AddScoped<IBenzeneServiceContainer>(_ => benzeneServiceContainer);
// var benzeneGrpc = new GrpcMethodHandlerFactory(benzeneServiceContainer, Greeter.Descriptor);
// builder.Services.AddScoped<IGrpcMethodHandlerFactory>(_ => benzeneGrpc);

        
        var pipeline = app.Create<BenzeneMessageContext>();
        action(pipeline);
       
        app.Register(x => x.AddSingleton<IGrpcMethodHandlerFactory>(_ =>
            new GrpcMethodHandlerFactory2(x, Greeter.Descriptor, pipeline.Build())));
        
        return app;
    }
    }


}

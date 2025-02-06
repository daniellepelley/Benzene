using Benzene.Abstractions.Middleware;
using Benzene.AspNet.Core;
using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Example.Grpc.Services
{
    public static class Extensions
    {
        public static IAspApplicationBuilder UseGrpc(this IAspApplicationBuilder app,
            Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>> action)
        {
            // builder.Services.AddGrpc(x => x.Interceptors.Add(typeof(BenzeneInterceptor)));
            // var benzeneServiceContainer = new MicrosoftBenzeneServiceContainer(builder.Services);
            // builder.Services.AddScoped<IBenzeneServiceContainer>(_ => benzeneServiceContainer);
            // var benzeneGrpc = new GrpcMethodHandlerFactory(benzeneServiceContainer, Greeter.Descriptor);
            // builder.Services.AddScoped<IGrpcMethodHandlerFactory>(_ => benzeneGrpc);


            var pipeline = app.Create<BenzeneMessageContext>();
            action(pipeline);

            // app.Register(x => x.AddSingleton<IGrpcMethodHandlerFactory>(_ =>
            //     new GrpcMethodHandlerFactory(x, Greeter.Descriptor, pipeline.Build())));:w
            

            return app;
        }
    }

}

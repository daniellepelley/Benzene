using Benzene.Abstractions.DI;
using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.Example.Grpc;
using Benzene.Example.Grpc.Services;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Services;
using Benzene.Grpc;
using Benzene.Microsoft.Dependencies;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc(x => x.Interceptors.Add(typeof(BenzeneInterceptor)));
var benzeneServiceContainer = new MicrosoftBenzeneServiceContainer(builder.Services);
builder.Services.AddScoped<IBenzeneServiceContainer>(_ => benzeneServiceContainer);

var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<GrpcContext>(benzeneServiceContainer);

var benzeneGrpc = new GrpcMethodHandlerFactory(benzeneServiceContainer, Greeter.Descriptor,
    middlewarePipelineBuilder.UseMessageHandlers().Build()); 
builder.Services.AddScoped<IGrpcMethodHandlerFactory>(_ => benzeneGrpc);

builder.Services.UsingBenzene(
    x => x.AddBenzene()
        .AddBenzeneMessage()
        .AddMessageHandlers(typeof(OrderDto).Assembly)
        .AddGrpc()
);
builder.Services.AddScoped<IOrderDbClient, InMemoryOrderDbClient>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();
// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
// app.UseBenzene(x => x.UseMessageRouter());
app.UseBenzene(x => x
    //.UseAspNet(asp => asp
    //     // .UseProcessResponseIfHandled()
    //     .UseMessageHandlers()
    // )
    .UseGrpc(grpc => grpc
        .UseMessageHandlers()
    )
);

app.Run();

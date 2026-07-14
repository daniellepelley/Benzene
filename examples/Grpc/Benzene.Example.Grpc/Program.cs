using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Example.Grpc.Handlers;
using Benzene.Example.Grpc.Services;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Services;
using Benzene.Grpc.AspNet;
using Benzene.Microsoft.Dependencies;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddBenzeneGrpc();
builder.Services.UsingBenzene(
    x => x.AddBenzene()
        .AddBenzeneMessage()
        .AddMessageHandlers(typeof(OrderDto).Assembly, typeof(SayHelloMessageHandler).Assembly)
        .AddGrpcMessageHandlers()
);
builder.Services.AddScoped<IOrderDbClient, InMemoryOrderDbClient>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();
// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.UseBenzene(x => x
    .UseGrpc(grpc => grpc
        .UseMessageHandlers()
    )
);

app.Run();

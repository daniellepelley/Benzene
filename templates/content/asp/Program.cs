using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Microsoft.Dependencies;
using BenzeneStarter;

var builder = WebApplication.CreateBuilder(args);

// Register Benzene and discover message handlers in this assembly. Add more handler classes
// alongside HelloWorldMessageHandler.cs - they're found automatically by reflection, no routing
// table to maintain.
builder.Services.UsingBenzene(x => x
    .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly));

var app = builder.Build();

// Add the Benzene HTTP pipeline: turn each request into a message and route it to a handler.
// This is the transport-specific wiring - it's the one part that changes if you move this
// handler to AWS Lambda or Azure Functions later (see docs/getting-started-aws.md /
// docs/azure-functions.md).
app.UseBenzene(benzene => benzene
    .UseHttp(http => http
        .UseMessageHandlers()));

app.Run();

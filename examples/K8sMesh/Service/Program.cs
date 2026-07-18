using Benzene.AspNet.Core;
using Benzene.Examples.K8sMesh.Service;

var builder = WebApplication.CreateBuilder(args);

// Listen on the port the container is given (Kubernetes probes/Service target this).
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.UseBenzene<Startup>();

var app = builder.Build();
app.UseBenzene();
app.Run();

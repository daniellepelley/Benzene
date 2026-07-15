using Benzene.AspNet.Core;
using Benzene.Examples.Google;

var builder = WebApplication.CreateBuilder(args);

// Cloud Run injects the port to listen on via the PORT env var - see
// https://cloud.google.com/run/docs/container-contract#port
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.UseBenzene<Startup>();

var app = builder.Build();

app.UseBenzene();

app.Run();

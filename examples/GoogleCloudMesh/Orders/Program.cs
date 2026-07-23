using Benzene.AspNet.Core;
using Benzene.Examples.GoogleCloudMesh.Orders;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
builder.UseBenzene<Startup>();
var app = builder.Build();
app.UseBenzene();
app.Run();

using Benzene.AspNet.Core;
using Benzene.Example.Cloudflare;

var builder = WebApplication.CreateBuilder(args);
builder.UseBenzene<StartUp>();

var app = builder.Build();
app.UseBenzene();

app.Run();

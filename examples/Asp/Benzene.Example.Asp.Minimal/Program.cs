using Benzene.AspNet.Core;
using Benzene.Example.Asp.Minimal;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Run StartUp.GetConfiguration + StartUp.ConfigureServices, stashing StartUp for the call below.
builder.UseBenzene<StartUp>();

var app = builder.Build();

// Run StartUp.Configure against the built pipeline, wiring Benzene into the request pipeline.
app.UseBenzene();

app.Run();

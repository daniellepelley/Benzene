# BenzeneStarter

A minimal [Benzene](https://github.com/daniellepelley/Benzene) service on ASP.NET Core, generated
from the `benzene.asp` template.

## Run it

```bash
dotnet run
```

```bash
curl http://localhost:5000/hello/world
# {"message":"Hello world!"}
```

## Where to go next

- **`HelloWorldMessageHandler.cs`** is where your logic goes - replace it, or add more handlers
  alongside it (they're discovered automatically by reflection).
- **`Program.cs`** wires Benzene into ASP.NET Core's request pipeline - you shouldn't need to touch
  this for a new handler.
- Full guide: [Getting Started](https://github.com/daniellepelley/Benzene/blob/main/docs/getting-started.md)
- Take the same handler to [AWS Lambda](https://github.com/daniellepelley/Benzene/blob/main/docs/getting-started-aws.md)
  or [Azure Functions](https://github.com/daniellepelley/Benzene/blob/main/docs/azure-functions.md)
  without changing a line of it - or start there directly with `dotnet new benzene.aws.apigateway`
  / `dotnet new benzene.azure.http`.

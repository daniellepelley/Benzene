# Spec

The spec topic allows a Hex service to serve up schemas such as OpenApi, AsyncApi and a custom format used to generate code.

This functionality can be added to a direct message pipeline using the UseSpec middleware extensions. The topic for this should be set to “spec”.


```csharp
  app.UseDirectMessage(directMessageApp => directMessageApp
      .UseSpec("spec")
```

## Making a Spec Request


```json
{
  "topic": "spec",
  "message" : "{\"type\":\"asyncapi\",\"format\":\"json\"}"
}
```
 
| Field | Options |
| ----- | ------- |
| Type  | “asyncapi”, ”openapi”, ”benzene” |
| Format | “json”, ”yaml” |


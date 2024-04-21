# Correlation Ids

This will create a correlation Id for the current request, either copying from the message headers if a correlation Id has been passed in, or by creating a new correlation Id if there is not an existing correlation Id.

```csharp
app.UseDirectMessage(directMessageApp => directMessageApp
   .UseCorrelationId()
);
```

The Correlation Id can be added to the Log Context using the following.

```csharp
.UseLogResult(x => x.WithCorrelationId());
```
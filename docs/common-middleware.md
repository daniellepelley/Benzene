# Common Middleware

## UseCorrelationId

This will attempt to pick up a Correlation Id from the message, usually this will be on the header as “x-correlation-id” or “correlation-id”, but this can be set in the middleware itself. It will create an instance of ICorrelationId which can then be injected into classes if you need to access the correlation id. The correlation id can be added to structured logs to help with tracing.

```csharp
.UseCorrelationId()
```

## UseTimer

This will create a timer with the name passed in and will write this to either the logs, X-Ray, or anything else you have configured.

```csharp
.UseTimer("direct-message-application")
```

## UseProcessResponse
This will create a response to be returns out of the service

```csharp
.UseProcessDirectMessageResponse()
```

## UseHealthCheck
This will allow healthchecks to be accessed using the topic added. By default “healthcheck” will always access the healthchecks on a service, but you might want call multiple healthchecks from outside the service so this give you the options to have a topic called something like “<service-name>:healthcheck”.

```csharp
.UseHealthCheck(healthCheckTopic, healthCheckBuilder)
```

## UseSpec
This allows you to query schemas from the service such as openapi, asyncapi and iris. It is essential that this is added if you want to use the Command line tools to generate code.

```csharp
.UseSpec("spec")
```

## UseMessageRouter
This is the middleware that will route the raw message to a message handler by pulling out the topic and deserializing the payload. You can add additional middleware to the message router such as validation and permissions.

```csharp
.UseMessageHandler(x => x
```

## UseFluentValidation
This adds FluentValidation to the pipeline. It will attempt to find a validator for the request type, and if it finds a validation failure it will return a validation failure before it even hits the message handler.

```csharp
.UseMessageRouter(x => x
    .UseFluentValidation())
```
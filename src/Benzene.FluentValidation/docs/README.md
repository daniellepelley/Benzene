# Fluent Validation
FluentValidation is a popular .NET library used for building strongly-typed validation rules. It allows developers to implement validation logic using a fluent interface and lambda expressions, resulting in clean and maintainable code. FluentValidation supports ASP.NET Core, MVC, Web API, and several other .NET platforms. It provides a wide range of built-in validators for common scenarios and also allows custom validators for complex validation logic. It can be easily integrated with your projects and offers options for automatic validation within the MVC pipeline. FluentValidation promotes the separation of concerns by keeping validation rules separate from business logic.

### Integration with Benzene
Fluent Validation can be added to the message router middleware, this is the point where the type of request has been resolved.

The fluent validation middleware will attempt to resolve a validator for the request type, and if successful it will validate the payload. If the request fails validation it will return a **ValidationError** result. If the either the payload is valid or no validator is found then the request will be forwarded on towards the message handler.


```csharp
.UseMessageRouter(router => router
    .UseFluentValidation()
);
```
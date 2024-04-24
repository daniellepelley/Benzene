# Data Annotations

System.ComponentModel.DataAnnotations is a namespace in .NET that provides a set of built-in validation attributes. These attributes can be applied to properties or classes to enforce data validation rules and define metadata for ASP.NET Core model binding and Entity Framework Core. This includes attributes for specifying required fields (Required), string length (StringLength), range of values (Range), regular expression patterns (RegularExpression), and more. It also provides a way to create custom validation attributes for more complex scenarios. This helps in maintaining data integrity and improving the robustness of your application.

### Integration with Benzene

Data Annotations can be added to the message router middleware, this is the point where the type of request has been resolved.

The data annotations middleware will validate for the request type using the data annotations on the request type. If the request fails validation it will return a ValidationError result. If the payload is valid then the request will be forwarded on towards the message handler.

```csharp
.UseMessageRouter(router => router
    .UseDataAnnotationsValidation()
);
```
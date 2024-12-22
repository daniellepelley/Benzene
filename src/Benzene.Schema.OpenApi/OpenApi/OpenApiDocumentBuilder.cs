using Benzene.Abstractions.Info;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Http.Routing;
using Benzene.Results;
using Benzene.Schema.OpenApi.Abstractions;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace Benzene.Schema.OpenApi.OpenApi;

public class OpenApiDocumentBuilder :
        IConsumesHttpEndpointDefinitions<OpenApiDocumentBuilder>,
        IConsumesApplicationInfo<OpenApiDocumentBuilder>,
        IProducesJson,
        IProducesYaml
{
    private readonly OpenApiPaths _paths = new();
    private readonly ISchemaBuilder _schemaBuilder = new SchemaBuilder();
    private OpenApiInfo _openApiInfo;
    private readonly List<OpenApiTag> _tags = new();

    public OpenApiDocumentBuilder(ISchemaBuilder? schemaBuilder = null)
    {
        if (schemaBuilder != null)
        {
            _schemaBuilder = schemaBuilder;
        }
    }

    public OpenApiDocument Build()
    {
        return new OpenApiDocument
        {
            Info = _openApiInfo,
            Tags = _tags.ToArray(),
            Paths = _paths,
            Components = new OpenApiComponents
            {
                Schemas = _schemaBuilder.Build().OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value)
            }
        };
    }

    public Dictionary<string, OpenApiSchema> GetSchemas()
    {
        return _schemaBuilder.Build();
    }

    public OpenApiDocumentBuilder AddApplicationInfo(IApplicationInfo applicationInfo)
    {
        return AddInfo(new OpenApiInfo
        {
            Title = applicationInfo.Name,
            Description = applicationInfo.Description,
            Version = applicationInfo.Version
        });
    }

    public OpenApiDocumentBuilder AddInfo(OpenApiInfo openApiInfo)
    {
        _openApiInfo = openApiInfo;
        return this;
    }

    public OpenApiDocumentBuilder AddTag(OpenApiTag openApiTag)
    {
        _tags.Add(openApiTag);
        return this;
    }

    public OpenApiDocumentBuilder AddHttpEndpointDefinitions(IHttpEndpointDefinition[] httpEndpointDefinitions,
        IMessageHandlerDefinition[] messageHandlerDefinitions)
    {
        var paths = httpEndpointDefinitions.GroupBy(GetPath);

        foreach (var route in paths)
        {
            AddHttpEndpointDefinition(route.Key, route.ToArray(), messageHandlerDefinitions);
        }

        return this;
    }

    public void AddHttpEndpointDefinition(string path, IHttpEndpointDefinition[] httpEndpointDefinitions,
        IMessageHandlerDefinition[] messageHandlerDefinitions)
    {
        _paths.Add(path, new OpenApiPathItem
        {
            Operations = httpEndpointDefinitions
                .Select(x => CreateOpenApiOperation(x, messageHandlerDefinitions))
                .ToDictionary(x => x.Key, x => x.Value)
        });
    }

    private static string GetPath(IHttpEndpointDefinition httpEndpointDefinitions)
    {
        var path = httpEndpointDefinitions.Path.ToLowerInvariant();
        return path.StartsWith("/") ? path : $"/{path}";
    }

    private KeyValuePair<OperationType, OpenApiOperation> CreateOpenApiOperation(IHttpEndpointDefinition httpEndpointDefinition, IMessageHandlerDefinition[] messageHandlerDefinitions)
    {
        var operationType = MapOperationType(httpEndpointDefinition.Method);

        var messageHandlerDefinition = messageHandlerDefinitions.First(h => h.Topic.Id == httpEndpointDefinition.Topic);


        var operation = new OpenApiOperation();

        operation.Parameters = CreateParameters(httpEndpointDefinition.Path);
        if (operationType != OperationType.Get)
        {
            operation.RequestBody = CreateRequestBody(messageHandlerDefinition.RequestType);
        }

        operation.Responses = CreateResponses(messageHandlerDefinition.ResponseType);

        return new KeyValuePair<OperationType, OpenApiOperation>(operationType, operation);
    }

    private static OpenApiParameter[] CreateParameters(string path)
    {
        var routeTemplate = TemplateParser.Parse(path);
        return routeTemplate.Parameters.Select(x => new OpenApiParameter
        {
            Name = x.Name,
            In = ParameterLocation.Path,
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = "string"
            }
        }).ToArray();
    }

    private OpenApiResponses CreateResponses(Type type)
    {
        var output = new OpenApiResponses();

        if (typeof(IBase64JsonMessage).IsAssignableFrom(type))
        {
            output.Add("200", CreateResponse(type, "application/base64", false));
        }
        else if (typeof(IRawStringMessage).IsAssignableFrom(type))
        {
            output.Add("200", CreateResponse(type, "application/string", false));
        }
        else
        {
            output.Add("200", CreateResponse(type, "application/json", true));
        }

        AddErrorResponses(output);

        return output;
    }

    private OpenApiResponse CreateResponse(Type type, string contextType, bool hasType)
    {
        return new OpenApiResponse
        {
            Description = type.Name,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                {
                    contextType, new OpenApiMediaType
                    {
                        Schema = hasType
                            ? _schemaBuilder.AddSchema(type)
                            : new OpenApiSchema
                            {
                                Type = "string"
                            }
                    }
                }
            }
        };
    }

    private OpenApiRequestBody CreateRequestBody(Type type)
    {
        return new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                {
                    "application/json", new OpenApiMediaType
                    {
                        Schema = _schemaBuilder.AddSchema(type),
                    }
                }
            }
        };
    }


    private static OperationType MapOperationType(string method)
    {
        if (string.IsNullOrEmpty(method))
        {
            return OperationType.Post;
        }

        var mappings = new Dictionary<string, OperationType>
        {
            { "GET", OperationType.Get },
            { "PUT", OperationType.Put },
            { "POST", OperationType.Post },
            { "DELETE", OperationType.Delete },
            { "OPTIONS", OperationType.Options },
            { "HEAD", OperationType.Head },
            { "PATCH", OperationType.Patch },
            { "TRACE", OperationType.Trace },
        };

        return mappings[method.ToUpperInvariant()];
    }

    // public OpenApiSchema AddSchema(Type type)
    // {
    //     _schemaBuilder.AddSchema(type);
    // }

    private void AddErrorResponses(OpenApiResponses openApiResponses)
    {
        var responsesDictionary = new Dictionary<string, string>
        {
            { "400", "400BadRequest" },
            { "401", "401Unauthorised" },
            { "403", "403Forbidden" },
            { "404", "404NotFound" },
            { "422", "422UnprocessableEntity" },
            { "500", "500InternalServerError" },
            { "503", "503ServiceUnavailable" }
        };

        foreach (var response in responsesDictionary)
        {
            openApiResponses.Add(response.Key, new OpenApiResponse
            {
                Description = response.Value,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    {
                        "application/json", new OpenApiMediaType
                        {
                            Schema = _schemaBuilder.AddSchema(typeof(ErrorPayload))
                        }
                    }
                }
            });
        }
    }

    public string GenerateJson()
    {
        return Build().SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
    }

    public string GenerateYaml()
    {
        return Build().SerializeAsYaml(OpenApiSpecVersion.OpenApi3_0);
    }
}

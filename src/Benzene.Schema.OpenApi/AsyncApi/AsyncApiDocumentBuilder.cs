using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Messages;
using Benzene.Schema.OpenApi.Abstractions;
using LEGO.AsyncAPI;
using LEGO.AsyncAPI.Models;
using Microsoft.OpenApi.Models;

namespace Benzene.Schema.OpenApi.AsyncApi;

public class AsyncApiDocumentBuilder :
    IConsumesMessageHandlerDefinitions<AsyncApiDocumentBuilder>,
    IConsumesBroadcastEventsDefinitions<AsyncApiDocumentBuilder>,
    IConsumesMessageSenderDefinitions<AsyncApiDocumentBuilder>,
    IConsumesApplicationInfo<AsyncApiDocumentBuilder>,
    IProducesJson,
    IProducesYaml
{
    private readonly ISchemaBuilder _schemaBuilder = new SchemaBuilder();
    private AsyncApiInfo _openApiInfo = new();
    private readonly List<AsyncApiTag> _tags = new();
    private readonly Dictionary<string, AsyncApiChannel> _channels = new();
    
    public AsyncApiDocumentBuilder(ISchemaBuilder? schemaBuilder = null)
    {
        if (schemaBuilder != null)
        {
            _schemaBuilder = schemaBuilder;
        }
    }

    public AsyncApiDocument Build()
    {

        return new AsyncApiDocument
        {
            Info = _openApiInfo,
            Tags = _tags.ToArray(),
            Channels = _channels,
            Components = new AsyncApiComponents
            {
                Schemas = _schemaBuilder.Build().ToDictionary(
                    x => x.Key,
                    x => Mapper.Map(x.Value))
            }
        };
    }
    public AsyncApiDocumentBuilder AddApplicationInfo(IApplicationInfo applicationInfo)
    {
        return AddInfo(new AsyncApiInfo
        {
            Title = applicationInfo.Name,
            Description = applicationInfo.Description,
            Version = applicationInfo.Version
        });
    }

    public AsyncApiDocumentBuilder AddInfo(AsyncApiInfo openApiInfo)
    {
        _openApiInfo = openApiInfo;
        return this;
    }

    public AsyncApiDocumentBuilder AddTag(AsyncApiTag openApiTag)
    {
        _tags.Add(openApiTag);
        return this;
    }

    public AsyncApiDocumentBuilder AddMessageHandlerDefinitions(IMessageHandlerDefinition[] messageHandlerDefinitions)
    {
        var messageDefinitionsDictionary = messageHandlerDefinitions.GroupBy(x => x.Topic.Id)
            .ToDictionary(x => x.Key, x => x.ToArray());
        
        foreach (var messageHandlerDefinition in messageDefinitionsDictionary)
        {
            AddMessageHandlerDefinition(messageHandlerDefinition.Key, messageHandlerDefinition.Value);
        }
        return this;
    }

    public void AddMessageHandlerDefinition(string topic, IMessageHandlerDefinition[] messageHandlerDefinitions)
    {
        _channels.Add(topic, new AsyncApiChannel
        {
            Subscribe = new AsyncApiOperation
            {
                OperationId = topic,
                Message = messageHandlerDefinitions.Select(x => 
                    CreateAsyncApiMessage(topic, x.Topic.Version, x.RequestType)).ToList(),
            }
        });

        _channels.Add($"{topic}:benzeneResult", new AsyncApiChannel
        {
            Publish = new AsyncApiOperation
            {
                OperationId = $"{topic}:benzeneResult",
                Message = messageHandlerDefinitions.Select(x => 
                    CreateAsyncApiMessage(topic, x.Topic.Version, x.ResponseType)).ToList()
            }
        });
    }

    private AsyncApiMessage CreateAsyncApiMessage(string topic, string version, Type payloadType)
    {
        return CreateAsyncApiMessage(topic, version, AddSchema(payloadType));
    }
    
    private AsyncApiMessage CreateAsyncApiMessage(string topic, string version, AsyncApiSchema schema)
    {
        var name = string.IsNullOrEmpty(version) ? topic : $"{topic} v{version}";
        return new AsyncApiMessage
        {
            Name = name,
            MessageId = name,
            Title = name,
            ContentType = "application/json",
            Payload = schema,
        };
    }

    public AsyncApiDocumentBuilder AddBroadcastEventDefinitions(IMessageDefinition[] messageDefinitions)
    {
        foreach (var messageDefinition in messageDefinitions)
        {
            AddBroadcastEventDefinition(messageDefinition);
        }
        return this;
    }

    public AsyncApiDocumentBuilder AddBroadcastEventDefinition(IMessageDefinition messageDefinition)
    {
        _channels.Add(messageDefinition.Topic.Id, new AsyncApiChannel
        {
            Publish = new AsyncApiOperation
            {
                OperationId = messageDefinition.Topic.Id,
                Summary = "Summary",
                Description = "Description",
                Message = new List<AsyncApiMessage>
                {
                    CreateAsyncApiMessage(messageDefinition.Topic.Id, "", messageDefinition.RequestType)
                }
            }
        });
        return this;
    }

    public AsyncApiDocumentBuilder AddEventDefinition(string topic, string typeName, OpenApiSchema schema)
    {
        _channels.Add(topic, new AsyncApiChannel
        {
            Publish = new AsyncApiOperation
            {
                OperationId = topic,
                Summary = "Summary",
                Description = "Description",
                Message = new List<AsyncApiMessage>
                {
                    CreateAsyncApiMessage(topic, String.Empty, AddSchema(typeName, schema))
                }
            }
        });
        return this;
    }

    public AsyncApiDocumentBuilder AddMessageSenderDefinitions(IMessageDefinition[] messageDefinitions)
    {
        foreach (var messageDefinition in messageDefinitions)
        {
            AddMessageSenderDefinition(messageDefinition);
        }

        return this;
    }
    
    
    public AsyncApiDocumentBuilder AddMessageSenderDefinition(IMessageDefinition messageDefinition)
    {
        _channels.Add(messageDefinition.Topic.Id, new AsyncApiChannel
        {
            Publish = new AsyncApiOperation
            {
                OperationId = messageDefinition.Topic.Id,
                Summary = "Summary",
                Description = "Description",
                Message = new List<AsyncApiMessage>
                {
                    CreateAsyncApiMessage(messageDefinition.Topic.Id, "", messageDefinition.RequestType)
                }
            }
        });
        return this;
    }

    public AsyncApiSchema AddSchema(string key, OpenApiSchema openApiSchema)
    {
        return Mapper.Map(_schemaBuilder.AddSchema(key, openApiSchema));
    }

    public AsyncApiSchema AddSchema(Type type)
    {
        return Mapper.Map(_schemaBuilder.AddSchema(type));
    }

    private string GetUniqueId(IMessageHandlerDefinition messageHandlerDefinition)
    {
        return $"{messageHandlerDefinition.Topic}{messageHandlerDefinition.Topic.Version}";
    }

    public string GenerateJson()
    {
        return Build().SerializeAsJson(AsyncApiVersion.AsyncApi2_0);
    }

    public string GenerateYaml()
    {
        return Build().SerializeAsYaml(AsyncApiVersion.AsyncApi2_0);
    }
}

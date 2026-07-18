using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Messages;
using Benzene.Http.Routing;
using Benzene.Schema.OpenApi.Abstractions;
using Benzene.Schema.OpenApi.Examples;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace Benzene.Schema.OpenApi.EventService
{
    public class EventServiceDocumentBuilder :
        IConsumesMessageHandlerDefinitions<EventServiceDocumentBuilder>,
        IConsumesBroadcastEventsDefinitions<EventServiceDocumentBuilder>,
        IConsumesMessageSenderDefinitions<EventServiceDocumentBuilder>,
        IConsumesApplicationInfo<EventServiceDocumentBuilder>,
        IConsumesHttpEndpointDefinitions<EventServiceDocumentBuilder>,
        IConsumesMessageEndpoint<EventServiceDocumentBuilder>,
        IProducesJson,
        IProducesYaml
    {
        private readonly List<Event> _events = new();
        private readonly List<RequestResponse> _requests = new();
        private readonly ISchemaBuilder _schemaBuilder = new SchemaBuilder();
        private OpenApiInfo _openApiInfo = new();
        private readonly List<OpenApiTag> _tags = new();
        private string? _messageEndpoint;

        public EventServiceDocumentBuilder(ISchemaBuilder? schemaBuilder = null)
        {
            if (schemaBuilder != null)
            {
                _schemaBuilder = schemaBuilder;
            }
        }

        public EventServiceDocument Build()
        {
            var components = new OpenApiComponents
            {
                Schemas = _schemaBuilder.Build()
            };

            AddGeneratedExamples(components.Schemas);

            return new EventServiceDocument(
                _openApiInfo,
                _tags.ToArray(),
                _requests.ToArray(),
                _events.ToArray(),
                components
            )
            {
                MessageEndpoint = _messageEndpoint
            };
        }

        /// <summary>
        /// Advertises the service's BenzeneMessage-over-HTTP endpoint as the document's top-level
        /// <c>messageEndpoint</c> field, so spec consumers (e.g. the Spec UI's try-it panel) can
        /// discover where to POST message envelopes.
        /// </summary>
        public EventServiceDocumentBuilder AddMessageEndpoint(string path)
        {
            _messageEndpoint = path;
            return this;
        }

        /// <summary>
        /// Populates each request/event with a deterministic example payload generated from its
        /// schema (see <see cref="ExamplePayloadBuilder"/>), unless an example was already
        /// supplied. Generation failures never fail the spec build — the example is simply
        /// omitted for that topic.
        /// </summary>
        private void AddGeneratedExamples(IDictionary<string, OpenApiSchema> schemas)
        {
            var schemaGetter = new SchemaGetter(schemas);
            var examplePayloadBuilder = new ExamplePayloadBuilder();

            foreach (var request in _requests.Where(x => x.Example == null && x.Request != null))
            {
                request.Example = TryBuildExample(examplePayloadBuilder, request.Request, schemaGetter);
            }

            foreach (var @event in _events.Where(x => x.Example == null && x.Message != null))
            {
                @event.Example = TryBuildExample(examplePayloadBuilder, @event.Message, schemaGetter);
            }
        }

        private static IOpenApiAny? TryBuildExample(IExamplePayloadBuilder examplePayloadBuilder,
            OpenApiSchema schema, ISchemaGetter schemaGetter)
        {
            try
            {
                return OpenApiAnyConverter.ToOpenApiAny(examplePayloadBuilder.Build(schema, schemaGetter));
            }
            catch
            {
                return null;
            }
        }

        public Dictionary<string, OpenApiSchema> GetSchemas()
        {
            return _schemaBuilder.Build();
        }

        public EventServiceDocumentBuilder AddApplicationInfo(IApplicationInfo applicationInfo)
        {
            return AddInfo(new OpenApiInfo
            {
                Title = applicationInfo.Name,
                Description = applicationInfo.Description,
                Version = applicationInfo.Version
            });
        }

        public EventServiceDocumentBuilder AddInfo(OpenApiInfo openApiInfo)
        {
            _openApiInfo = openApiInfo;
            return this;
        }

        public EventServiceDocumentBuilder AddTag(OpenApiTag openApiTag)
        {
            _tags.Add(openApiTag);
            return this;
        }
        public EventServiceDocumentBuilder AddMessageHandlerDefinitions(IMessageHandlerDefinition[] messageHandlerDefinitions)
        {
            foreach (var messageHandlerDefinition in messageHandlerDefinitions)
            {
                AddMessageHandlerDefinition(messageHandlerDefinition);
            }
            return this;
        }

        public void AddMessageHandlerDefinition(IMessageHandlerDefinition messageHandlerDefinition)
        {
            if (!_requests.Any(x => x.Topic == messageHandlerDefinition.Topic.Id && x.Version == messageHandlerDefinition.Topic.Version))
            {
                _requests.Add(new RequestResponse
                {
                    Topic = messageHandlerDefinition.Topic.Id,
                    Version = messageHandlerDefinition.Topic.Version,
                    Reserved = ReservedTopics.IsReserved(messageHandlerDefinition.Topic.Id),
                    Request = _schemaBuilder.AddSchema(messageHandlerDefinition.RequestType),
                    Response = _schemaBuilder.AddSchema(messageHandlerDefinition.ResponseType)
                });
            }
        }

        public EventServiceDocumentBuilder AddBroadcastEventDefinitions(IMessageDefinition[] messageDefinitions)
        {
            foreach (var messageDefinition in messageDefinitions)
            {
                AddBroadcastEventDefinition(messageDefinition);
            }
            return this;
        }

        public EventServiceDocumentBuilder AddBroadcastEventDefinition(IMessageDefinition messageDefinition)
        {
            _events.Add(new Event
            (
                messageDefinition.Topic.Id,
                _schemaBuilder.AddSchema(messageDefinition.RequestType)
            ));
            return this;
        }
        public EventServiceDocumentBuilder AddMessageSenderDefinitions(IMessageDefinition[] messageDefinitions)
        {
            foreach (var messageDefinition in messageDefinitions)
            {
                AddMessageSenderDefinition(messageDefinition);
            }

            return this;
        }

        public EventServiceDocumentBuilder AddMessageSenderDefinition(IMessageDefinition messageDefinition)
        {
            return AddEvent(messageDefinition.Topic.Id, messageDefinition.RequestType);
        }

        public EventServiceDocumentBuilder AddEvent(string topic, Type type)
        {
            _events.Add(new Event
            (
                topic,
                _schemaBuilder.AddSchema(type)
            ));
            return this;
        }

        public EventServiceDocumentBuilder AddEventDefinition(string topic, string payloadName, OpenApiSchema schema)
        {
            _events.Add(new Event
            (
                topic,
                _schemaBuilder.AddSchema(payloadName, schema)
            ));
            return this;
        }

        public OpenApiSchema AddSchema(string key, OpenApiSchema schema)
        {
            return _schemaBuilder.AddSchema(key, schema);
        }

        public string GenerateJson()
        {
            return Build().SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
        }

        public string GenerateYaml()
        {
            return Build().SerializeAsYaml(OpenApiSpecVersion.OpenApi3_0);
        }

        public EventServiceDocumentBuilder AddHttpEndpointDefinitions(IHttpEndpointDefinition[] httpEndpointDefinitions,
            IMessageHandlerDefinition[] messageHandlerDefinitions)
        {
            AddMessageHandlerDefinitions(messageHandlerDefinitions);

            foreach (var group in httpEndpointDefinitions.GroupBy(x => x.Topic))
            {
                var request = _requests.FirstOrDefault(x => x.Topic == group.First().Topic);
                if (request != null)
                {
                    request.HttpMappings = group.Select(x =>
                        new HttpMapping
                        {
                            Method = x.Method,
                            Path = x.Path
                        }).ToArray();
                }
            }

            return this;
        }
    }
}

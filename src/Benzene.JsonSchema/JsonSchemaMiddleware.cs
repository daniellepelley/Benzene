using System.Text.Json;
using System.Text.Json.Nodes;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Results;
using Json.Schema;

namespace Benzene.JsonSchema;

public class JsonSchemaMiddleware<TContext> : IMiddleware<TContext> where TContext : class
{
    private readonly IMessageBodyGetter<TContext> _messageBodyGetter;
    private readonly IJsonSchemaProvider<TContext> _jsonSchemaProvider;
    private readonly IDefaultStatuses _defaultStatuses;
    private IMessageHandlerResultSetter<TContext> _messageHandlerResultSetter;

    public JsonSchemaMiddleware(IMessageBodyGetter<TContext> messageBodyGetter, IJsonSchemaProvider<TContext> jsonSchemaProvider, IDefaultStatuses defaultStatuses, IMessageHandlerResultSetter<TContext> messageHandlerResultSetter)
    {
        _messageHandlerResultSetter = messageHandlerResultSetter;
        _defaultStatuses = defaultStatuses;
        _jsonSchemaProvider = jsonSchemaProvider;
        _messageBodyGetter = messageBodyGetter;
    }

    public string Name => "JsonSchema";

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var jsonSchema = _jsonSchemaProvider.Get(context);

        if (jsonSchema == null)
        {
            await next();
            return;
        }
        
        var body = _messageBodyGetter.GetBody(context);

        if (body == null)
        {
            _messageHandlerResultSetter.SetResultAsync(context,
                new MessageHandlerResult(BenzeneResult.Set(_defaultStatuses.ValidationError, false)));
            return;
        }

        var jsonNode = JsonSerializer.Deserialize<JsonNode>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var schemaResult = jsonSchema.Evaluate(jsonNode, new EvaluationOptions
        {
            OutputFormat = OutputFormat.List
        });

        if (schemaResult.IsValid)
        {
            await next();
        }
        else
        {
            _messageHandlerResultSetter.SetResultAsync(context,
                new MessageHandlerResult(BenzeneResult.Set(_defaultStatuses.ValidationError, false)));
        }
    }
}
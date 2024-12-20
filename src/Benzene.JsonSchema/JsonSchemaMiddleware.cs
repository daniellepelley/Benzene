﻿using System.Text.Json;
using System.Text.Json.Nodes;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Json.Schema;

namespace Benzene.JsonSchema;

public class JsonSchemaMiddleware<TContext> : IMiddleware<TContext> where TContext : class, IHasMessageResult
{
    private readonly IMessageBodyMapper<TContext> _messageBodyMapper;
    private readonly IJsonSchemaProvider<TContext> _jsonSchemaProvider;
    private IDefaultStatuses _defaultStatuses;

    public JsonSchemaMiddleware(IMessageBodyMapper<TContext> messageBodyMapper, IJsonSchemaProvider<TContext> jsonSchemaProvider, IDefaultStatuses defaultStatuses)
    {
        _defaultStatuses = defaultStatuses;
        _jsonSchemaProvider = jsonSchemaProvider;
        _messageBodyMapper = messageBodyMapper;
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
        
        var body = _messageBodyMapper.GetBody(context);

        if (body == null)
        {
            context.MessageResult = MessageResult.Failure(_defaultStatuses.ValidationError);
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
            context.MessageResult = MessageResult.Failure(_defaultStatuses.ValidationError);
        }
    }
}
using System.Text.Json;
using Benzene.Abstractions.MessageHandlers;
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
    private readonly IMessageHandlerResultSetter<TContext> _messageHandlerResultSetter;
    private readonly IMessageTopicGetter<TContext> _messageTopicGetter;
    private readonly IMessageHandlerDefinitionLookUp _messageHandlerDefinitionLookUp;

    public JsonSchemaMiddleware(IMessageBodyGetter<TContext> messageBodyGetter,
        IJsonSchemaProvider<TContext> jsonSchemaProvider,
        IDefaultStatuses defaultStatuses,
        IMessageHandlerResultSetter<TContext> messageHandlerResultSetter,
        IMessageTopicGetter<TContext> messageTopicGetter,
        IMessageHandlerDefinitionLookUp messageHandlerDefinitionLookUp)
    {
        _messageHandlerResultSetter = messageHandlerResultSetter;
        _messageTopicGetter = messageTopicGetter;
        _messageHandlerDefinitionLookUp = messageHandlerDefinitionLookUp;
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
            await SetValidationErrorAsync(context, JsonSchemaValidationErrors.MissingBody);
            return;
        }

        JsonDocument jsonDocument;
        try
        {
            jsonDocument = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            // Malformed JSON is the most clearly-invalid body of all - treat it as a validation
            // failure (like a null or schema-failing body) rather than letting the exception
            // escape the pipeline as an internal error. Mirrors IsJsonValidator.
            await SetValidationErrorAsync(context, JsonSchemaValidationErrors.MalformedBody);
            return;
        }

        using (jsonDocument)
        {
            var schemaResult = jsonSchema.Evaluate(jsonDocument.RootElement, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List
            });

            if (schemaResult.IsValid)
            {
                await next();
            }
            else
            {
                await SetValidationErrorAsync(context, JsonSchemaValidationErrors.Format(schemaResult));
            }
        }
    }

    private Task SetValidationErrorAsync(TContext context, params string[] errors)
    {
        // Same failure contract as Benzene.FluentValidation/Benzene.DataAnnotations: the messages
        // travel as the result's errors, which the response pipeline serializes as an ErrorPayload
        // ({ status, errors }). The topic's handler definition is attached so the response payload
        // mapper actually writes that body (it skips definition-less results).
        var topic = _messageTopicGetter.GetTopic(context);
        var messageHandlerDefinition = topic != null ? _messageHandlerDefinitionLookUp.FindHandler(topic) : null;

        return _messageHandlerResultSetter.SetResultAsync(context,
            new MessageHandlerResult(topic, messageHandlerDefinition,
                BenzeneResult.Set(_defaultStatuses.ValidationError, errors)));
    }
}

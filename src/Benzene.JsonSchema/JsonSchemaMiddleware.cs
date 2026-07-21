using System.Text.Json;
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

        JsonDocument? jsonDocument = null;
        if (body != null)
        {
            try
            {
                jsonDocument = JsonDocument.Parse(body);
            }
            catch (JsonException)
            {
                // Malformed JSON is the most clearly-invalid body of all - treat it as a validation
                // failure (like a null or schema-failing body) rather than letting the exception
                // escape the pipeline as an internal error. Mirrors IsJsonValidator.
                jsonDocument = null;
            }
        }

        if (jsonDocument == null)
        {
            await _messageHandlerResultSetter.SetResultAsync(context,
                new MessageHandlerResult(BenzeneResult.Set(_defaultStatuses.ValidationError, false)));
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
                await _messageHandlerResultSetter.SetResultAsync(context,
                    new MessageHandlerResult(BenzeneResult.Set(_defaultStatuses.ValidationError, false)));
            }
        }
    }
}
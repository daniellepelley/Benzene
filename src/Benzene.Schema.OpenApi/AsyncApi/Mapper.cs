using LEGO.AsyncAPI.Models;
using LEGO.AsyncAPI.Models.Any;
using LEGO.AsyncAPI.Models.Interfaces;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using ReferenceType = Microsoft.OpenApi.Models.ReferenceType;

namespace Benzene.Schema.OpenApi.AsyncApi
{
    public static class Mapper
    {
        public static AsyncApiSchema? Map(OpenApiSchema? input)
        {
            if (input == null)
            {
                return null;
            }

            return new AsyncApiSchema
            {
                Type = MapType(input.Type),
                Reference = MapReference(input.Reference),
                Items = Map(input.Items),
                Properties = input.Properties.ToDictionary(x => x.Key, x => Map(x.Value)),
                Format = input.Format,
                Description = input.Description,
                MaxLength = input.MaxLength,
                MinLength = input.MinLength,
                Required = input.Required,
                Pattern = input.Pattern,
                Nullable = input.Nullable,
                Enum = input.Enum.Select(MapOpenApiAny).ToList(),
            };
        }

        public static IAsyncApiAny MapOpenApiAny(IOpenApiAny openApiAny)
        {
            switch (openApiAny)
            {
                case OpenApiInteger openApiInteger:
                    return new AsyncApiInteger(openApiInteger.Value);
                case OpenApiString openApiString:
                    return new AsyncApiString(openApiString.Value);
                case OpenApiBoolean openApiBoolean:
                    return new AsyncApiBoolean(openApiBoolean.Value);
                case OpenApiLong openApiLong:
                    return new AsyncApiLong(openApiLong.Value);
                case OpenApiFloat openApiFloat:
                    return new AsyncApiFloat(openApiFloat.Value);
                case OpenApiDouble openApiDouble:
                    return new AsyncApiDouble(openApiDouble.Value);
                case OpenApiByte openApiByte:
                    return new AsyncApiByte(openApiByte.Value);
                case OpenApiBinary openApiBinary:
                    return new AsyncApiBinary(openApiBinary.Value);
                case OpenApiDate openApiDate:
                    return new AsyncApiDate(openApiDate.Value);
                case OpenApiDateTime openApiDateTime:
                    return new AsyncApiDateTime(openApiDateTime.Value);
                default:
                    throw new ArgumentException($"Unsupported IOpenApiAny type: {openApiAny.GetType()}");
            }
        }


        public static SchemaType MapType(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return SchemaType.Null;
            }

           var mappings = new Dictionary<string, SchemaType>
            {
                { "null", SchemaType.Null },
                { "boolean",SchemaType.Boolean },
                { "object", SchemaType.Object },
                { "array", SchemaType.Array },
                { "number", SchemaType.Number },
                { "string", SchemaType.String },
                { "integer", SchemaType.Integer },
            };

            return mappings[type];
        }

        public static AsyncApiReference? MapReference(OpenApiReference? input)
        {
            if (input == null)
            {
                return null;
            }

            return new AsyncApiReference
            {
                Id = input.Id,
                // HostDocument = input.HostDocument,
                Type = MapReferenceType(input.Type),
            };
        }

        public static LEGO.AsyncAPI.Models.ReferenceType MapReferenceType(ReferenceType? type)
        {
            if (!type.HasValue)
            {
                return LEGO.AsyncAPI.Models.ReferenceType.None;
            }

            var mappings = new Dictionary<ReferenceType, LEGO.AsyncAPI.Models.ReferenceType>
            {
                { ReferenceType.Schema, LEGO.AsyncAPI.Models.ReferenceType.Schema},
            };

            //         None,
            // [Display("schemas")] Schema,
            // [Display("servers")] Server,
            // [Display("channels")] Channel,
            // [Display("messages")] Message,
            // [Display("securitySchemes")] SecurityScheme,
            // [Display("parameters")] Parameter,
            // [Display("correlationIds")] CorrelationId,
            // [Display("operationTraits")] OperationTrait,
            // [Display("messageTraits")] MessageTrait,
            // [Display("serverBindings")] ServerBindings,
            // [Display("channelBindings")] ChannelBindings,
            // [Display("operationBindings")] OperationBindings,
            // [Display("messageBindings")] MessageBindings,
            // [Display("examples")] Example,
            // [Display("headers")] Header,
            // [Display("serverVariable")] ServerVariable,

            return mappings[type.Value];
        }

    }
}

using Benzene.CodeGen.Core;
using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Client
{
    public class CSharpTypeName : ITypeName
    {
        public string GetName(OpenApiSchema openApiSchema)
        {
            if (openApiSchema == null)
            {
                return "Void";
            }
            
            if (openApiSchema.Reference != null && !string.IsNullOrEmpty(openApiSchema.Reference.Id))
            {
                return openApiSchema.Reference.Id;
            }

            if (openApiSchema.Type == "array")
            {
                var type = GetArrayType(openApiSchema.Items);
                return $"{type}[]";
            }

            if (openApiSchema.Type == "string" && openApiSchema.Format == "date-time")
            {
                return "DateTime?";
            }

            if (openApiSchema.Type == "string" && openApiSchema.Format == "uuid")
            {
                return "Guid?";
            }

            // A free-form object (a map) is modelled by additionalProperties. Guard the null case first:
            // a plain "object" schema with no additionalProperties leaves the property null, so reading
            // .Type off it threw a NullReferenceException. When it is present, type the dictionary value
            // from it (string -> Dictionary<string, string>, an int64 map -> Dictionary<string, long>,
            // a $ref -> Dictionary<string, Thing>) instead of only recognising the string case.
            if (openApiSchema.Type == "object" && openApiSchema.AdditionalProperties != null)
            {
                var valueType = GetName(openApiSchema.AdditionalProperties);
                if (!string.IsNullOrEmpty(valueType))
                {
                    return $"Dictionary<string, {valueType}>";
                }
            }

            if (openApiSchema.Type == "integer")
            {
                // An int64-format integer must map to long, not int, or generated clients silently
                // truncate 64-bit ids/amounts.
                var integerType = openApiSchema.Format == "int64" ? "long" : "int";
                return openApiSchema.Nullable ? $"{integerType}?" : integerType;
            }

            if (openApiSchema.Type == "number")
            {
                return openApiSchema.Nullable ? "double?" : "double";
            }

            if (openApiSchema.Type == "boolean")
            {
                return "bool";
            }

            return openApiSchema.Type;
        }

        private string GetArrayType(OpenApiSchema openApiSchema)
        {
            if (string.IsNullOrEmpty(openApiSchema.Type) || openApiSchema.Type == "object")
            {
                return openApiSchema.Reference.Id;
            }

            return GetName(openApiSchema);
        }
    }
}

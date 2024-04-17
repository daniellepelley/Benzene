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

            if (openApiSchema.Type == "object" && openApiSchema.AdditionalProperties.Type == "string")
            {
                return "Dictionary<string, string>";
            }

            if (openApiSchema.Type == "integer")
            {
                return openApiSchema.Nullable ? "int?" : "int";
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

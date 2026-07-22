using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Benzene.Azure.Function.SourceGenerators
{
    /// <summary>Reads <c>[assembly: BenzeneHttpTrigger(...)]</c> declarations into <see cref="TriggerInfo"/>.</summary>
    internal static class Http
    {
        public static ImmutableArray<TriggerInfo> Read(GeneratorAttributeSyntaxContext context)
        {
            var builder = ImmutableArray.CreateBuilder<TriggerInfo>();

            foreach (var attribute in context.Attributes)
            {
                // Named-property defaults declared on the attribute class don't surface in AttributeData
                // unless explicitly set, so the generator applies the same defaults (Name = "benzene",
                // catch-all route) - keeping a bare [assembly: BenzeneHttpTrigger] working zero-config.
                var name = AttributeReading.NamedString(attribute, "Name", "benzene");
                var route = AttributeReading.NamedString(attribute, "Route", "{*restOfPath}");
                var methods = AttributeReading.NamedStringArrayCsv(attribute, "Methods", "get", "post", "put", "delete", "options");
                var authLevel = AttributeReading.NamedEnumMember(
                    attribute, "AuthorizationLevel", "global::Microsoft.Azure.Functions.Worker.AuthorizationLevel.Anonymous");

                var binding =
                    $"global::Microsoft.Azure.Functions.Worker.HttpTrigger({authLevel}, {methods}, Route = {AttributeReading.Literal(route)})";

                builder.Add(new TriggerInfo(
                    className: AttributeReading.ToIdentifier(name) + "HttpFunction",
                    functionNameLiteral: AttributeReading.Literal(name),
                    bindingAttribute: binding,
                    parameterType: "global::Microsoft.AspNetCore.Http.HttpRequest",
                    parameterName: "req",
                    returnType: "global::System.Threading.Tasks.Task<global::Microsoft.AspNetCore.Mvc.IActionResult>",
                    dispatchExpression: "global::Benzene.Azure.Function.AspNet.Extensions.HandleHttpRequest(_app, req)"));
            }

            return builder.ToImmutable();
        }
    }
}

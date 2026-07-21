using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Benzene.CodeGen.SourceGenerators
{
    [Generator]
    public class MessageHandlerSourceGenerator : IIncrementalGenerator
    {
        public static readonly DiagnosticDescriptor DuplicateTopic = new(
            id: "BENZ001",
            title: "Duplicate message topic",
            messageFormat: "Message topic '{0}'{1} is handled by multiple handlers: {2}",
            category: "Benzene",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                    transform: (ctx, _) => GetMessageHandlerInfo(ctx))
                .Where(t => t is not null);

            // Drive the output straight off the collected handlers. Combining with CompilationProvider
            // (as this used to) re-runs the whole output on every compilation - i.e. every keystroke -
            // defeating the incremental generator's caching, and the Compilation was never even used by
            // Execute. With the handler model now a value-equality record, an unchanged set of handlers
            // produces an equal collected array, so RegisterSourceOutput's cache short-circuits.
            context.RegisterSourceOutput(provider.Collect(), (spc, handlers) => Execute(spc, handlers!));
        }

        private static MessageHandlerInfo? GetMessageHandlerInfo(GeneratorSyntaxContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

            if (symbol is not INamedTypeSymbol typeSymbol)
                return null;

            var messageAttribute = typeSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "Benzene.Core.MessageHandlers.MessageAttribute" || 
                                     a.AttributeClass?.Name == "MessageAttribute" || 
                                     a.AttributeClass?.Name == "Message");

            if (messageAttribute == null)
                return null;

            var interfaces = typeSymbol.AllInterfaces;
            var handlerInterface = interfaces.FirstOrDefault(i =>
                i.IsGenericType && (
                    i.ConstructedFrom.ToString() == "Benzene.Abstractions.MessageHandlers.IMessageHandler<TRequest, TResponse>" ||
                    i.ConstructedFrom.ToString() == "Benzene.Abstractions.MessageHandlers.IMessageHandler<TRequest>"));

            if (handlerInterface == null)
                return null;

            var topic = "";
            var version = "";

            if (messageAttribute.ConstructorArguments.Length > 0)
            {
                topic = messageAttribute.ConstructorArguments[0].Value?.ToString() ?? "";
            }
            else
            {
                var topicArg = messageAttribute.NamedArguments.FirstOrDefault(a => a.Key == "Topic");
                if (topicArg.Key != null)
                {
                    topic = topicArg.Value.Value?.ToString() ?? "";
                }
            }

            if (messageAttribute.ConstructorArguments.Length > 1)
            {
                version = messageAttribute.ConstructorArguments[1].Value?.ToString() ?? "";
            }
            else
            {
                var versionArg = messageAttribute.NamedArguments.FirstOrDefault(a => a.Key == "Version");
                if (versionArg.Key != null)
                {
                    version = versionArg.Value.Value?.ToString() ?? "";
                }
            }

            var requestType = handlerInterface.TypeArguments[0].ToDisplayString();
            var responseType = handlerInterface.TypeArguments.Length > 1 
                ? handlerInterface.TypeArguments[1].ToDisplayString() 
                : "Benzene.Abstractions.Results.Void";

            return new MessageHandlerInfo(
                topic,
                version,
                requestType,
                responseType,
                typeSymbol.ToDisplayString(),
                classDeclaration.Identifier.GetLocation()
            );
        }

        private static void Execute(SourceProductionContext context, ImmutableArray<MessageHandlerInfo> handlers)
        {
            if (handlers.IsDefaultOrEmpty)
                return;

            var duplicateGroups = handlers
                .GroupBy(h => new { h.Topic, h.Version })
                .Where(g => g.Count() > 1);

            foreach (var group in duplicateGroups)
            {
                var versionText = string.IsNullOrEmpty(group.Key.Version) ? "" : $" (version '{group.Key.Version}')";
                var handlerNames = string.Join(", ", group.Select(h => h.HandlerFullType));
                foreach (var handler in group)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DuplicateTopic, handler.Location, group.Key.Topic, versionText, handlerNames));
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("using Benzene.Abstractions.DI;");
            sb.AppendLine("using Benzene.Abstractions.MessageHandlers;");
            sb.AppendLine("using Benzene.Core.MessageHandlers;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("");
            sb.AppendLine("namespace Benzene.Core.MessageHandlers.DI");
            sb.AppendLine("{");
            sb.AppendLine("    public static class BenzeneGeneratedHandlersExtensions");
            sb.AppendLine("    {");
            sb.AppendLine("        public static IBenzeneServiceContainer AddGeneratedMessageHandlers(this IBenzeneServiceContainer services)");
            sb.AppendLine("        {");
            sb.AppendLine("            var list = services.GetService<MessageHandlersList>();");
            sb.AppendLine("            if (list == null)");
            sb.AppendLine("            {");
            sb.AppendLine("                list = new MessageHandlersList();");
            sb.AppendLine("                services.AddSingleton<MessageHandlersList>(list);");
            sb.AppendLine("                services.AddSingleton<IMessageHandlersList>(list);");
            sb.AppendLine("            }");
            sb.AppendLine("");

            foreach (var handler in handlers)
            {
                // Topic/Version come from a user-authored [Message] attribute, so they can contain a
                // quote, backslash or other character that would break (or inject into) the generated
                // string literal. FormatLiteral produces a correctly-escaped, quoted C# literal.
                var topicLiteral = SymbolDisplay.FormatLiteral(handler.Topic, quote: true);
                var versionLiteral = SymbolDisplay.FormatLiteral(handler.Version, quote: true);
                sb.AppendLine($"            services.AddScoped<{handler.HandlerFullType}>();");
                sb.AppendLine($"            list.Add(MessageHandlerDefinition.CreateInstance({topicLiteral}, {versionLiteral}, typeof({handler.RequestFullType}), typeof({handler.ResponseFullType}), typeof({handler.HandlerFullType})));");
            }

            sb.AppendLine("");
            sb.AppendLine("            return services;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource("BenzeneGeneratedHandlers.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }

    // Value equality is what lets the incremental pipeline cache: RegisterSourceOutput re-runs only
    // when the collected array of these actually differs. As a plain reference-equality class every
    // syntax pass produced "different" instances and the generator re-ran every time. Location is
    // included so a duplicate diagnostic still moves with the code, at the cost of a cache miss when a
    // handler's line shifts - still far better than never caching.
    internal sealed class MessageHandlerInfo : IEquatable<MessageHandlerInfo>
    {
        public string Topic { get; }
        public string Version { get; }
        public string RequestFullType { get; }
        public string ResponseFullType { get; }
        public string HandlerFullType { get; }
        public Location Location { get; }

        public MessageHandlerInfo(string topic, string version, string requestFullType, string responseFullType, string handlerFullType, Location location)
        {
            Topic = topic;
            Version = version;
            RequestFullType = requestFullType;
            ResponseFullType = responseFullType;
            HandlerFullType = handlerFullType;
            Location = location;
        }

        public bool Equals(MessageHandlerInfo? other)
        {
            return other is not null
                && Topic == other.Topic
                && Version == other.Version
                && RequestFullType == other.RequestFullType
                && ResponseFullType == other.ResponseFullType
                && HandlerFullType == other.HandlerFullType
                && Location.Equals(other.Location);
        }

        public override bool Equals(object? obj) => Equals(obj as MessageHandlerInfo);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + (Topic?.GetHashCode() ?? 0);
                hash = hash * 31 + (Version?.GetHashCode() ?? 0);
                hash = hash * 31 + (RequestFullType?.GetHashCode() ?? 0);
                hash = hash * 31 + (ResponseFullType?.GetHashCode() ?? 0);
                hash = hash * 31 + (HandlerFullType?.GetHashCode() ?? 0);
                hash = hash * 31 + (Location?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}

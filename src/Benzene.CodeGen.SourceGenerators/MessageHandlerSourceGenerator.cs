using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Benzene.CodeGen.SourceGenerators
{
    [Generator]
    public class MessageHandlerSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                    transform: (ctx, _) => GetMessageHandlerInfo(ctx))
                .Where(t => t is not null);

            var compilation = context.CompilationProvider.Combine(provider.Collect());

            context.RegisterSourceOutput(compilation, (spc, source) => Execute(spc, source.Left, source.Right!));
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
                typeSymbol.ToDisplayString()
            );
        }

        private static void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<MessageHandlerInfo> handlers)
        {
            if (handlers.IsDefaultOrEmpty)
                return;

            var duplicates = handlers
                .GroupBy(h => new { h.Topic, h.Version })
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var duplicate in duplicates)
            {
                // In a real scenario, we would report a diagnostic here
                // context.ReportDiagnostic(Diagnostic.Create(...));
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
                sb.AppendLine($"            services.AddScoped<{handler.HandlerFullType}>();");
                sb.AppendLine($"            list.Add(MessageHandlerDefinition.CreateInstance(\"{handler.Topic}\", \"{handler.Version}\", typeof({handler.RequestFullType}), typeof({handler.ResponseFullType}), typeof({handler.HandlerFullType})));");
            }

            sb.AppendLine("");
            sb.AppendLine("            return services;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource("BenzeneGeneratedHandlers.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }

    internal class MessageHandlerInfo
    {
        public string Topic { get; }
        public string Version { get; }
        public string RequestFullType { get; }
        public string ResponseFullType { get; }
        public string HandlerFullType { get; }

        public MessageHandlerInfo(string topic, string version, string requestFullType, string responseFullType, string handlerFullType)
        {
            Topic = topic;
            Version = version;
            RequestFullType = requestFullType;
            ResponseFullType = responseFullType;
            HandlerFullType = handlerFullType;
        }
    }
}

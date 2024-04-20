using System.Reflection;
using Benzene.Clients;
using Benzene.CodeGen;
using Benzene.Core.MessageHandling;
using Benzene.Examples.App.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Benzene.Examples.CodeGen.Client
{
    public class Class1
    {
        [Fact]
        public void Fact()
        {
            var finder = new ReflectionMessageHandlersFinder(typeof(OrderDto).Assembly);
            var messageHandlerDefinitions = finder.FindHandlers();

            var builder = new Benzene.CodeGen.LambdaServiceSdkBuilder("demo-lambda", "OrderService", "Benzene.Examples.Clients");

            var dictionary = builder.Build(new ServiceDefinition
            {
                MessageDefinitions = messageHandlerDefinitions.Select(x => new MessageDefinition
                {
                    MessageHandlerDefinition = x
                }).ToArray()
            });

            CreateAssemblyDefinition(dictionary.Values.ToArray());
        }

        public static void CreateAssemblyDefinition(string[] codeFiles)
        {        
            IReadOnlyCollection<MetadataReference> _references = new[] {
                MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ValueTuple<>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ClientResponse).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Task<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IDisposable).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=4.2.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location)
            };
       
            var sourceLanguage = new CSharpLanguage();

            var syntaxTrees = codeFiles.Select(code => sourceLanguage.ParseText(code, SourceCodeKind.Regular)).ToArray();

            Compilation compilation = sourceLanguage
                .CreateLibraryCompilation(assemblyName: "InMemoryAssembly", enableOptimisations: false)
                .AddReferences(_references)
                .AddSyntaxTrees(syntaxTrees);

            // var stream = new MemoryStream();
            // var emitResult = compilation.Emit(stream);

            var emitResult = compilation.Emit($"{Directory.GetCurrentDirectory()}\\some.dll");
            if (emitResult.Success)
            {
                // stream.Seek(0, SeekOrigin.Begin);

            }

        }
    }

    public interface ILanguageService
    {
        SyntaxTree ParseText(string code, SourceCodeKind kind);
        Compilation CreateLibraryCompilation(string assemblyName, bool enableOptimisations);
    }

    public class CSharpLanguage : ILanguageService
    {
        private static readonly LanguageVersion MaxLanguageVersion = Enum
            .GetValues(typeof(LanguageVersion))
            .Cast<LanguageVersion>()
            .Max();

        private readonly IReadOnlyCollection<MetadataReference> _references = new[] {
            MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ValueTuple<>).GetTypeInfo().Assembly.Location)
        };

        public SyntaxTree ParseText(string sourceCode, SourceCodeKind kind)
        {
            var options = new CSharpParseOptions(kind: kind, languageVersion: MaxLanguageVersion);

            // Return a syntax tree of our source code
            return CSharpSyntaxTree.ParseText(sourceCode, options);
        }

        public Compilation CreateLibraryCompilation(string assemblyName, bool enableOptimisations)
        {
            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: enableOptimisations ? OptimizationLevel.Release : OptimizationLevel.Debug,
                allowUnsafe: true);

            return CSharpCompilation.Create(assemblyName, options: options, references: _references);
        }
    }
}
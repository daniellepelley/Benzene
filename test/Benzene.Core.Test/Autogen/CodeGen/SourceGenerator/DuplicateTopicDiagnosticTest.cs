using System;
using System.Linq;
using Benzene.CodeGen.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.SourceGenerator;

public class DuplicateTopicDiagnosticTest
{
    private const string HandlerTemplate = @"
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;

namespace TestNamespace
{{
    public class Request {{ }}
    public class Response {{ }}

    [Message(""{0}"", ""{1}"")]
    public class FirstHandler : IMessageHandler<Request, Response>
    {{
        public System.Threading.Tasks.Task<Benzene.Abstractions.Results.IBenzeneResult<Response>> HandleAsync(Request request) => null;
    }}

    [Message(""{2}"", ""{3}"")]
    public class SecondHandler : IMessageHandler<Request, Response>
    {{
        public System.Threading.Tasks.Task<Benzene.Abstractions.Results.IBenzeneResult<Response>> HandleAsync(Request request) => null;
    }}
}}";

    private static GeneratorRunResult RunGenerator(string source)
    {
        // Force-load the assemblies the generator's semantic checks need, then
        // reference everything in the test AppDomain so the source compiles.
        _ = typeof(global::Benzene.Abstractions.MessageHandlers.IMessageHandler<,>);
        _ = typeof(global::Benzene.Core.MessageHandlers.MessageAttribute);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic && !string.IsNullOrEmpty(x.Location))
            .Select(x => MetadataReference.CreateFromFile(x.Location));

        var compilation = CSharpCompilation.Create(
            "SourceGeneratorTest",
            new[] { CSharpSyntaxTree.ParseText(source) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new MessageHandlerSourceGenerator().AsSourceGenerator());
        return ((CSharpGeneratorDriver)driver.RunGenerators(compilation))
            .GetRunResult().Results.Single();
    }

    [Fact]
    public void DuplicateTopicAndVersion_ReportsBenz001Error()
    {
        var result = RunGenerator(string.Format(HandlerTemplate, "order:create", "v1", "order:create", "v1"));

        var diagnostics = result.Diagnostics.Where(x => x.Id == "BENZ001").ToArray();
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, x => Assert.Equal(DiagnosticSeverity.Error, x.Severity));
        var message = diagnostics[0].GetMessage();
        Assert.Contains("order:create", message);
        Assert.Contains("FirstHandler", message);
        Assert.Contains("SecondHandler", message);
    }

    [Fact]
    public void SameTopicDifferentVersion_NoDiagnostic()
    {
        var result = RunGenerator(string.Format(HandlerTemplate, "order:create", "v1", "order:create", "v2"));

        Assert.DoesNotContain(result.Diagnostics, x => x.Id == "BENZ001");
    }

    [Fact]
    public void DistinctTopics_NoDiagnostic_AndHandlersGenerated()
    {
        var result = RunGenerator(string.Format(HandlerTemplate, "order:create", "v1", "order:delete", "v1"));

        Assert.DoesNotContain(result.Diagnostics, x => x.Id == "BENZ001");
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("order:create", generated);
        Assert.Contains("order:delete", generated);
    }
}

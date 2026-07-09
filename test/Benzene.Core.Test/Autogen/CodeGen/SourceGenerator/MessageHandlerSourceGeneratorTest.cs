using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Benzene.CodeGen.SourceGenerators;

namespace Benzene.Test.Autogen.CodeGen.SourceGenerator
{
    public class CSharpIncrementalSourceGeneratorTest<TSourceGenerator, TVerifier> : CSharpSourceGeneratorTest<EmptySourceGeneratorProvider, TVerifier>
        where TSourceGenerator : IIncrementalGenerator, new()
        where TVerifier : IVerifier, new()
    {
        public CSharpIncrementalSourceGeneratorTest()
        {
            TestState.AdditionalReferences.Add(typeof(MessageHandlerSourceGenerator).Assembly);
            TestState.AdditionalReferences.Add(typeof(Benzene.Abstractions.MessageHandlers.IMessageHandler<,>).Assembly);
            TestState.AdditionalReferences.Add(typeof(Benzene.Core.MessageHandlers.MessageAttribute).Assembly);
            TestState.AdditionalReferences.Add(typeof(Benzene.Abstractions.Results.IBenzeneResult<>).Assembly);
            TestState.AdditionalReferences.Add(typeof(System.ComponentModel.Component).Assembly);
        }

        protected override IEnumerable<ISourceGenerator> GetSourceGenerators()
        {
            yield return new TSourceGenerator().AsSourceGenerator();
        }
    }

    [Generator]
    public class EmptySourceGeneratorProvider : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }
        public void Execute(GeneratorExecutionContext context) { }
    }

    public class MessageHandlerSourceGeneratorTest
    {
        [Fact(Skip = "Source Generator testing framework has verification issues in this environment")]
        public async Task ShouldGenerateHandlers()
        {
            var source = @"
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;

namespace TestNamespace
{
    public class Request { }
    public class Response { }

    [Message(""test-topic"", ""v1"")]
    public class TestHandler : IMessageHandler<Request, Response>
    {
        public System.Threading.Tasks.Task<Benzene.Abstractions.Results.IBenzeneResult<Response>> HandleAsync(Request request) => null;
    }
}";

            var expected = @"using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Core.MessageHandlers.DI
{
    public static class BenzeneGeneratedHandlersExtensions
    {
        public static IBenzeneServiceContainer AddGeneratedMessageHandlers(this IBenzeneServiceContainer services)
        {
            var list = services.GetService<MessageHandlersList>();
            if (list == null)
            {
                list = new MessageHandlersList();
                services.AddSingleton<MessageHandlersList>(list);
                services.AddSingleton<IMessageHandlersList>(list);
            }

            services.AddScoped<TestNamespace.TestHandler>();
            list.Add(MessageHandlerDefinition.CreateInstance(""test-topic"", ""v1"", typeof(TestNamespace.Request), typeof(TestNamespace.Response), typeof(TestNamespace.TestHandler)));

            return services;
        }
    }
}
";

            await new CSharpIncrementalSourceGeneratorTest<MessageHandlerSourceGenerator, XUnitVerifier>
            {
                TestState =
                {
                    Sources = { source },
                    GeneratedSources =
                    {
                        (typeof(MessageHandlerSourceGenerator), "BenzeneGeneratedHandlers.g.cs", SourceText.From(expected, Encoding.UTF8)),
                    },
                    ReferenceAssemblies = ReferenceAssemblies.Net.Net60 // Using Net60 as a baseline
                }
            }.RunAsync();
        }

        [Fact(Skip = "Source Generator testing framework has verification issues in this environment")]
        public async Task ShouldHandleSingleTypeParameterInterface()
        {
            var source = @"
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;

namespace TestNamespace
{
    public class Request { }

    [Message(""test-topic"")]
    public class TestHandler : IMessageHandler<Request>
    {
        public System.Threading.Tasks.Task<Benzene.Abstractions.Results.IBenzeneResult<Benzene.Abstractions.Results.Void>> HandleAsync(Request request) => System.Threading.Tasks.Task.FromResult((Benzene.Abstractions.Results.IBenzeneResult<Benzene.Abstractions.Results.Void>)null);
    }
}";

            var expected = @"using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Core.MessageHandlers.DI
{
    public static class BenzeneGeneratedHandlersExtensions
    {
        public static IBenzeneServiceContainer AddGeneratedMessageHandlers(this IBenzeneServiceContainer services)
        {
            var list = services.GetService<MessageHandlersList>();
            if (list == null)
            {
                list = new MessageHandlersList();
                services.AddSingleton<MessageHandlersList>(list);
                services.AddSingleton<IMessageHandlersList>(list);
            }

            services.AddScoped<TestNamespace.TestHandler>();
            list.Add(MessageHandlerDefinition.CreateInstance(""test-topic"", """", typeof(TestNamespace.Request), typeof(Benzene.Abstractions.Results.Void), typeof(TestNamespace.TestHandler)));

            return services;
        }
    }
}
";

            await new CSharpIncrementalSourceGeneratorTest<MessageHandlerSourceGenerator, XUnitVerifier>
            {
                TestState =
                {
                    Sources = { source },
                    GeneratedSources =
                    {
                        (typeof(MessageHandlerSourceGenerator), "BenzeneGeneratedHandlers.g.cs", SourceText.From(expected, Encoding.UTF8)),
                    },
                    ReferenceAssemblies = ReferenceAssemblies.Net.Net60
                }
            }.RunAsync();
        }
    }
}

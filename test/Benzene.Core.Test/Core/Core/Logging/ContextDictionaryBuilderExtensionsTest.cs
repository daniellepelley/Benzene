using System.Collections.Generic;
using Benzene.Core.Logging;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Core.Core.Logging;

public class ContextDictionaryBuilderExtensionsTest
{
    [Fact]
    public void With_StaticKeyValue_AddsToDictionary()
    {
        var builder = new ContextDictionaryBuilder<string>();

        builder.With("key", "value");

        var result = builder.Build(ServiceResolverMother.CreateServiceResolver(), "context");

        Assert.Equal("value", result["key"]);
    }

    [Fact]
    public void With_StaticDictionary_AddsToDictionary()
    {
        var builder = new ContextDictionaryBuilder<string>();

        builder.With(new Dictionary<string, string> { { "key", "value" } });

        var result = builder.Build(ServiceResolverMother.CreateServiceResolver(), "context");

        Assert.Equal("value", result["key"]);
    }

    [Fact]
    public void With_KeyAndResolverFunc_AddsToDictionary()
    {
        var builder = new ContextDictionaryBuilder<string>();

        builder.With("key", _ => "value");

        var result = builder.Build(ServiceResolverMother.CreateServiceResolver(), "context");

        Assert.Equal("value", result["key"]);
    }

    [Fact]
    public void With_ResolverFuncReturningDictionary_AddsToDictionary()
    {
        var builder = new ContextDictionaryBuilder<string>();

        builder.With(_ => new Dictionary<string, string> { { "key", "value" } });

        var result = builder.Build(ServiceResolverMother.CreateServiceResolver(), "context");

        Assert.Equal("value", result["key"]);
    }

    [Fact]
    public void With_KeyAndResolverContextFunc_AddsToDictionary()
    {
        var builder = new ContextDictionaryBuilder<string>();

        builder.With("key", (_, context) => context);

        var result = builder.Build(ServiceResolverMother.CreateServiceResolver(), "context-value");

        Assert.Equal("context-value", result["key"]);
    }

    [Fact]
    public void With_ResolverContextFuncReturningDictionary_AddsToDictionary()
    {
        var builder = new ContextDictionaryBuilder<string>();

        // Called via the static class explicitly: this extension method has the same
        // signature as IContextDictionaryBuilder<T>.With, so normal instance-method
        // syntax always resolves to the interface method instead of the extension.
        ContextDictionaryBuilderExtensions.With(builder, (_, context) => new Dictionary<string, string> { { "key", context } });

        var result = builder.Build(ServiceResolverMother.CreateServiceResolver(), "context-value");

        Assert.Equal("context-value", result["key"]);
    }
}

using System.Collections.Generic;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.Messages.Predicates;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Core.Core.Predicates;

public class MediaTypeHeaderContextPredicateTest
{
    private class TestContext
    {
        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();
    }

    private class TestHeadersGetter : IMessageHeadersGetter<TestContext>
    {
        public IDictionary<string, string> GetHeaders(TestContext context) => context.Headers;
    }

    private static IServiceResolver BuildResolver()
    {
        var services = ServiceResolverMother.CreateServiceCollection(x =>
            x.AddScoped<IMessageHeadersGetter<TestContext>, TestHeadersGetter>());

        return new MicrosoftServiceResolverFactory(services).CreateScope();
    }

    [Theory]
    [InlineData("application/xml", true)]
    [InlineData("application/xml; charset=utf-8", true)]
    [InlineData("APPLICATION/XML", true)]
    [InlineData("application/json", false)]
    public void Check_MatchesMediaTypeIgnoringParametersAndCase(string headerValue, bool expected)
    {
        var predicate = new MediaTypeHeaderContextPredicate<TestContext>("content-type", "application/xml");
        var context = new TestContext();
        context.Headers["content-type"] = headerValue;

        Assert.Equal(expected, predicate.Check(context, BuildResolver()));
    }

    [Fact]
    public void Check_MissingHeader_ReturnsFalse()
    {
        var predicate = new MediaTypeHeaderContextPredicate<TestContext>("content-type", "application/xml");
        var context = new TestContext();

        Assert.False(predicate.Check(context, BuildResolver()));
    }

    [Fact]
    public void Check_ExactEqualityStillWorks_ForPlainHeaderContextPredicate()
    {
        // The base HeaderContextPredicate's default comparison is unchanged (exact match) - only
        // MediaTypeHeaderContextPredicate opts into parameter/case-tolerant matching.
        var predicate = new HeaderContextPredicate<TestContext>("content-type", "application/xml");
        var context = new TestContext();
        context.Headers["content-type"] = "application/xml; charset=utf-8";

        Assert.False(predicate.Check(context, BuildResolver()));
    }
}

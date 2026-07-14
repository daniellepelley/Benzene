using System;
using System.Collections.Generic;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Request;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class EnrichingRequestMapperTest
{
    private class TestContext { }

    private class TestRequest
    {
        public string Name { get; set; }
    }

    private class FixedRequestMapper : IRequestMapper<TestContext>
    {
        private readonly object _request;

        public FixedRequestMapper(object request)
        {
            _request = request;
        }

        public TRequest GetBody<TRequest>(TestContext context) where TRequest : class => _request as TRequest;
    }

    private class TrackingEnricher : IRequestEnricher<TestContext>
    {
        private readonly IDictionary<string, object> _values;

        public TrackingEnricher(IDictionary<string, object> values)
        {
            _values = values;
        }

        public bool WasCalled { get; private set; }

        public IDictionary<string, object> Enrich<TRequest>(TRequest request, TestContext context)
        {
            WasCalled = true;
            return _values;
        }
    }

    [Fact]
    public void GetBody_InnerMapperReturnsNull_ReturnsNullWithoutCallingEnrichers()
    {
        var enricher = new TrackingEnricher(new Dictionary<string, object> { { "name", "should-not-apply" } });
        var mapper = new EnrichingRequestMapper<TestContext>(
            new FixedRequestMapper(null),
            new[] { enricher });

        var result = mapper.GetBody<TestRequest>(new TestContext());

        Assert.Null(result);
        Assert.False(enricher.WasCalled);
    }

    [Fact]
    public void GetBody_NoEnrichersRegistered_ReturnsMappedRequestUnchanged()
    {
        var request = new TestRequest { Name = "original" };
        var mapper = new EnrichingRequestMapper<TestContext>(
            new FixedRequestMapper(request),
            Array.Empty<IRequestEnricher<TestContext>>());

        var result = mapper.GetBody<TestRequest>(new TestContext());

        Assert.Same(request, result);
        Assert.Equal("original", result.Name);
    }

    [Fact]
    public void GetBody_EarlierEnricherTakesPrecedenceOverLater()
    {
        var request = new TestRequest();
        var mapper = new EnrichingRequestMapper<TestContext>(
            new FixedRequestMapper(request),
            new IRequestEnricher<TestContext>[]
            {
                new TrackingEnricher(new Dictionary<string, object> { { "name", "from-first" } }),
                new TrackingEnricher(new Dictionary<string, object> { { "name", "from-second" } })
            });

        var result = mapper.GetBody<TestRequest>(new TestContext());

        Assert.Equal("from-first", result.Name);
    }

    [Fact]
    public void GetBody_LaterEnricherFillsPropertyEarlierOneDidNotSet()
    {
        var request = new TestRequest();
        var mapper = new EnrichingRequestMapper<TestContext>(
            new FixedRequestMapper(request),
            new IRequestEnricher<TestContext>[]
            {
                new TrackingEnricher(new Dictionary<string, object>()),
                new TrackingEnricher(new Dictionary<string, object> { { "name", "from-second" } })
            });

        var result = mapper.GetBody<TestRequest>(new TestContext());

        Assert.Equal("from-second", result.Name);
    }
}

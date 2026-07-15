using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Microsoft.Dependencies;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Benchmarks;

/// <summary>
/// The payload type used by <see cref="RequestMappingBenchmarks"/> - a small, locally-defined POCO
/// so this project doesn't need a reference to the <c>test/</c> project's example types.
/// </summary>
public class BenchmarkRequestPayload
{
    public string? Name { get; set; }
    public int Id { get; set; }
}

/// <summary>
/// Benchmarks <see cref="MultiSerializerOptionsRequestMapper{TContext}.GetBody{TRequest}"/> against
/// <see cref="BenzeneMessageContext"/> (Benzene's reference transport). Two variants, deliberately
/// not conflated: <see cref="GetBody_FirstCall"/> measures a mapper's first call (media-format
/// negotiation plus building/caching its serializer-specific mapper pair);
/// <see cref="GetBody_WarmedCache"/> primes that cache with an unmeasured call before the measured
/// one, showing steady-state deserialization cost alone. A fresh
/// <see cref="MultiSerializerOptionsRequestMapper{TContext}"/> is constructed inside each
/// <c>[Benchmark]</c> method (not reused from <c>[GlobalSetup]</c>) because that's genuinely how it's
/// used in production - it's a scoped, per-message instance - so reusing one across iterations would
/// misrepresent the real allocation pattern.
/// </summary>
[MemoryDiagnoser]
public class RequestMappingBenchmarks
{
    private IServiceResolver _serviceResolver = null!;
    private IMediaFormatNegotiator<BenzeneMessageContext> _mediaFormatNegotiator = null!;
    private BenzeneMessageContext _context = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();
        services.UsingBenzene(x => x.AddBenzeneMessage());

        _serviceResolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        _mediaFormatNegotiator = _serviceResolver.GetService<IMediaFormatNegotiator<BenzeneMessageContext>>();
        _context = new BenzeneMessageContext(new BenzeneMessageRequest { Body = "{\"name\":\"orders-api\",\"id\":42}" });
    }

    private MultiSerializerOptionsRequestMapper<BenzeneMessageContext> CreateMapper()
        => new(_mediaFormatNegotiator, _serviceResolver, new BenzeneMessageGetter(),
            Array.Empty<IRequestEnricher<BenzeneMessageContext>>());

    [Benchmark(Description = "GetBody: first call (negotiate + build mapper cache)")]
    public BenchmarkRequestPayload? GetBody_FirstCall()
        => CreateMapper().GetBody<BenchmarkRequestPayload>(_context);

    [Benchmark(Description = "GetBody: warmed cache (steady-state deserialization)")]
    public BenchmarkRequestPayload? GetBody_WarmedCache()
    {
        var mapper = CreateMapper();
        mapper.GetBody<BenchmarkRequestPayload>(_context); // priming call, not measured
        return mapper.GetBody<BenchmarkRequestPayload>(_context);
    }
}

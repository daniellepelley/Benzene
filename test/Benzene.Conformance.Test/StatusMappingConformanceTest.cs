using System.Net;
using Benzene.Clients.Common;
using Benzene.Grpc;
using Benzene.Grpc.Client;
using Benzene.Http;
using Benzene.Results;
using Grpc.Core;
using Xunit;

namespace Benzene.Conformance.Test;

public class MappingFixture
{
    public List<MappingRow> Forward { get; set; } = new();
    public List<MappingRow> Reverse { get; set; } = new();
}

public class MappingRow
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}

/// <summary>
/// Runs docs/specification/conformance/http-status-mapping.json against the .NET HTTP mappers
/// (wire-contracts.md section 4.1).
/// </summary>
public class HttpStatusMappingConformanceTest
{
    private static readonly Lazy<MappingFixture> Fixture = new(() =>
        ConformanceFixtures.Load<MappingFixture>("http-status-mapping.json"));

    public static IEnumerable<object[]> ForwardRows() => Fixture.Value.Forward.Select(x => new object[] { x.From, x.To });

    public static IEnumerable<object[]> ReverseRows() => Fixture.Value.Reverse.Select(x => new object[] { x.From, x.To });

    [Theory]
    [MemberData(nameof(ForwardRows))]
    public void Forward_BenzeneStatusMapsToHttpStatusCode(string benzeneStatus, string expectedHttpCode)
    {
        var mapper = new DefaultHttpStatusCodeMapper();
        var input = benzeneStatus == "<unknown>" ? "SomeUnknownStatus" : benzeneStatus;

        Assert.Equal(expectedHttpCode, mapper.Map(input));
    }

    [Theory]
    [MemberData(nameof(ReverseRows))]
    public void Reverse_HttpStatusCodeMapsToBenzeneStatus(string httpCode, string expectedBenzeneStatus)
    {
        var result = ((HttpStatusCode)int.Parse(httpCode)).Convert();

        Assert.Equal(expectedBenzeneStatus, result.Status);
    }
}

/// <summary>
/// Runs docs/specification/conformance/grpc-status-mapping.json against the .NET gRPC mappers
/// (wire-contracts.md section 4.2). The benzene-status trailer-wins rule is covered by
/// Benzene.Grpc.Test's DefaultGrpcStatusReverseMapperTest.
/// </summary>
public class GrpcStatusMappingConformanceTest
{
    private static readonly Lazy<MappingFixture> Fixture = new(() =>
        ConformanceFixtures.Load<MappingFixture>("grpc-status-mapping.json"));

    public static IEnumerable<object[]> ForwardRows() => Fixture.Value.Forward.Select(x => new object[] { x.From, x.To });

    public static IEnumerable<object[]> ReverseRows() => Fixture.Value.Reverse.Select(x => new object[] { x.From, x.To });

    [Theory]
    [MemberData(nameof(ForwardRows))]
    public void Forward_BenzeneStatusMapsToGrpcStatusCode(string benzeneStatus, string expectedGrpcCode)
    {
        var mapper = new DefaultGrpcStatusCodeMapper();
        var input = benzeneStatus == "<unknown>" ? "SomeUnknownStatus" : benzeneStatus;

        Assert.Equal(Enum.Parse<StatusCode>(expectedGrpcCode), mapper.Map(input));
    }

    [Theory]
    [MemberData(nameof(ReverseRows))]
    public void Reverse_GrpcStatusCodeMapsToBenzeneStatus(string grpcCode, string expectedBenzeneStatus)
    {
        var mapper = new DefaultGrpcStatusReverseMapper();

        Assert.Equal(expectedBenzeneStatus, mapper.Map(Enum.Parse<StatusCode>(grpcCode), trailers: null));
    }
}

/// <summary>
/// Runs docs/specification/conformance/status-vocabulary.json against the .NET status constants
/// and success classification (wire-contracts.md section 3).
/// </summary>
public class StatusVocabularyConformanceTest
{
    public class VocabularyFixture
    {
        public List<VocabularyRow> Statuses { get; set; } = new();
    }

    public class VocabularyRow
    {
        public string Status { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
    }

    private static readonly Lazy<VocabularyFixture> Fixture = new(() =>
        ConformanceFixtures.Load<VocabularyFixture>("status-vocabulary.json"));

    [Fact]
    public void Vocabulary_MatchesTheBenzeneResultStatusConstants()
    {
        var constants = typeof(BenzeneResultStatus)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(x => x.IsLiteral)
            .Select(x => (string)x.GetRawConstantValue()!)
            .OrderBy(x => x)
            .ToArray();

        var fixtureStatuses = Fixture.Value.Statuses.Select(x => x.Status).OrderBy(x => x).ToArray();

        Assert.Equal(fixtureStatuses, constants);
    }

    public static IEnumerable<object[]> VocabularyRows() =>
        Fixture.Value.Statuses.Select(x => new object[] { x.Status, x.IsSuccess });

    [Theory]
    [MemberData(nameof(VocabularyRows))]
    public void Vocabulary_SuccessClassificationMatches(string status, bool isSuccess)
    {
        Assert.Equal(isSuccess, BenzeneResultHttpMapper.IsSuccessStatus(status));
        Assert.Equal(isSuccess, BenzeneResultStatus.IsSuccess(status));
        Assert.Equal(!isSuccess, BenzeneResultStatus.IsFailure(status));
        Assert.True(BenzeneResultStatus.IsKnown(status));
    }
}

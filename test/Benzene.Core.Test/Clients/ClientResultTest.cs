using Benzene.Results;
using Benzene.Test.Clients.Samples;
using Xunit;

namespace Benzene.Test.Clients;

public class BenzeneResultTest
{
    [Fact]
    public void Ok()
    {
        var result = BenzeneResult.Ok(new ExamplePayload());
        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
    }

    [Fact]
    public void Accepted()
    {
        var result = BenzeneResult.Accepted(new ExamplePayload());
        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }
    
    [Fact]
    public void Accepted_T()
    {
        var result = BenzeneResult.Accepted<ExamplePayload>();
        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }

    [Fact]
    public void Created()
    {
        var result = BenzeneResult.Created(new ExamplePayload());
        Assert.Equal(BenzeneResultStatus.Created, result.Status);
    }

    [Fact]
    public void Ignored()
    {
        var result = BenzeneResult.Ignored();
        Assert.Equal(BenzeneResultStatus.Ignored, result.Status);
    }

    [Fact]
    public void Conflict()
    {
        var result = BenzeneResult.Conflict<ExamplePayload>();
        Assert.Equal(BenzeneResultStatus.Conflict, result.Status);
    }

    [Fact]
    public void NotFound()
    {
        var result = BenzeneResult.NotFound<ExamplePayload>();
        Assert.Equal(BenzeneResultStatus.NotFound, result.Status);
    }

    [Fact]
    public void ServiceUnavailable()
    {
        var result = BenzeneResult.ServiceUnavailable<ExamplePayload>();
        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
    }

    [Fact]
    public void NotImplemented()
    {
        var result = BenzeneResult.NotImplemented<ExamplePayload>();
        Assert.Equal(BenzeneResultStatus.NotImplemented, result.Status);
    }

    [Fact]
    public void UnexpectedError()
    {
        var result = BenzeneResult.UnexpectedError<ExamplePayload>();
        Assert.Equal(BenzeneResultStatus.UnexpectedError, result.Status);
    }

    [Fact]
    public void ValidationError()
    {
        var result = BenzeneResult.ValidationError<ExamplePayload>();
        Assert.Equal(BenzeneResultStatus.ValidationError, result.Status);
    }
}

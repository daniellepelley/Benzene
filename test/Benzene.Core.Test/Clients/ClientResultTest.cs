using Benzene.Results;
using Benzene.Test.Clients.Samples;
using Xunit;

namespace Benzene.Test.Clients;

public class ClientResultTest
{
    [Fact]
    public void Success()
    {
        var result = ClientResult.Success(new ExamplePayload());
        Assert.Equal(ClientResultStatus.Success, result.Status);
    }

    [Fact]
    public void Ok()
    {
        var result = ClientResult.Ok(new ExamplePayload());
        Assert.Equal(ClientResultStatus.Ok, result.Status);
    }

    [Fact]
    public void Accepted()
    {
        var result = ClientResult.Accepted(new ExamplePayload());
        Assert.Equal(ClientResultStatus.Accepted, result.Status);
    }
    
    [Fact]
    public void Accepted_T()
    {
        var result = ClientResult.Accepted<ExamplePayload>();
        Assert.Equal(ClientResultStatus.Accepted, result.Status);
    }

    [Fact]
    public void Created()
    {
        var result = ClientResult.Created(new ExamplePayload());
        Assert.Equal(ClientResultStatus.Created, result.Status);
    }

    [Fact]
    public void Ignored()
    {
        var result = ClientResult.Ignored();
        Assert.Equal(ClientResultStatus.Ignored, result.Status);
    }

    [Fact]
    public void Conflict()
    {
        var result = ClientResult.Conflict<ExamplePayload>();
        Assert.Equal(ClientResultStatus.Conflict, result.Status);
    }

    [Fact]
    public void NotFound()
    {
        var result = ClientResult.NotFound<ExamplePayload>();
        Assert.Equal(ClientResultStatus.NotFound, result.Status);
    }

    [Fact]
    public void ServiceUnavailable()
    {
        var result = ClientResult.ServiceUnavailable<ExamplePayload>();
        Assert.Equal(ClientResultStatus.ServiceUnavailable, result.Status);
    }

    [Fact]
    public void NotImplemented()
    {
        var result = ClientResult.NotImplemented<ExamplePayload>();
        Assert.Equal(ClientResultStatus.NotImplemented, result.Status);
    }

    [Fact]
    public void UnexpectedError()
    {
        var result = ClientResult.UnexpectedError<ExamplePayload>();
        Assert.Equal(ClientResultStatus.UnexpectedError, result.Status);
    }

    [Fact]
    public void ValidationError()
    {
        var result = ClientResult.ValidationError<ExamplePayload>();
        Assert.Equal(ClientResultStatus.ValidationError, result.Status);
    }
}

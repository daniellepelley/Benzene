using Benzene.Core.Results;
using Xunit;

namespace Benzene.Examples.Aws.Tests.Unit.Services;

public class ClientResultExtensionsTests
{
    [Fact]
    public void Success()
    {
        var HandlerResult = ClientResult.Ok().AsHandlerResult();
        Assert.Equal(DefaultHandlerResultStatus.Success, HandlerResult.Status);
    }

    [Fact]
    public void Created()
    {
        var HandlerResult = ClientResult.Created().AsHandlerResult();
        Assert.Equal(DefaultHandlerResultStatus.Created, HandlerResult.Status);
    }

    [Fact]
    public void Updated()
    {
        var HandlerResult = ClientResult.Updated().AsHandlerResult();
        Assert.Equal(DefaultHandlerResultStatus.Updated, HandlerResult.Status);
    }

    [Fact]
    public void Deleted()
    {
        var HandlerResult = ClientResult.Deleted().AsHandlerResult();
        Assert.Equal(DefaultHandlerResultStatus.Deleted, HandlerResult.Status);
    }

    [Fact]
    public void NotFound()
    {
        var HandlerResult = ClientResult.NotFound().AsHandlerResult();
        Assert.Equal(DefaultHandlerResultStatus.NotFound, HandlerResult.Status);
    }

    [Fact]
    public void ServiceUnavailable()
    {
        var HandlerResult = ClientResult.ServiceUnavailable().AsHandlerResult();
        Assert.Equal(DefaultHandlerResultStatus.ServiceUnavailable, HandlerResult.Status);
    }

    [Fact]
    public void Success_T()
    {
        var HandlerResult = ClientResult.Ok<NullPayload>().AsHandlerResult();
        Assert.Equal(DefaultHandlerResultStatus.Success, HandlerResult.Status);
    }

    [Fact]
    public void Created_T()
    {
        var HandlerResult = ClientResult.Created<NullPayload>().AsHandlerResult();
        Assert.Equal(DefaultHandlerResultStatus.Created, HandlerResult.Status);
    }

    [Fact]
    public void Updated_T()
    {
        var HandlerResult = ClientResult.Updated<NullPayload>().AsHandlerResult();
        Assert.Equal(DefaultHandlerResultStatus.Updated, HandlerResult.Status);
    }

    [Fact]
    public void Deleted_T()
    {
        var HandlerResult = ClientResult.Deleted<NullPayload>().AsHandlerResult();
        Assert.Equal(DefaultHandlerResultStatus.Deleted, HandlerResult.Status);
    }

    [Fact]
    public void NotFound_T()
    {
        var HandlerResult = ClientResult.NotFound<NullPayload>().AsHandlerResult();
        Assert.Equal(DefaultHandlerResultStatus.NotFound, HandlerResult.Status);
    }

    [Fact]
    public void ServiceUnavailable_T()
    {
        var HandlerResult = ClientResult.ServiceUnavailable<NullPayload>().AsHandlerResult();
        Assert.Equal(DefaultHandlerResultStatus.ServiceUnavailable, HandlerResult.Status);
    }

    [Fact]
    public void Success_ResultT()
    {
        var HandlerResult = ClientResult.Ok().AsHandlerResult<NullPayload>();
        Assert.Equal(DefaultHandlerResultStatus.Success, HandlerResult.Status);
    }

    [Fact]
    public void Created_ResultT()
    {
        var HandlerResult = ClientResult.Created().AsHandlerResult<NullPayload>();
        Assert.Equal(DefaultHandlerResultStatus.Created, HandlerResult.Status);
    }

    [Fact]
    public void Updated_ResultT()
    {
        var HandlerResult = ClientResult.Updated().AsHandlerResult<NullPayload>();
        Assert.Equal(DefaultHandlerResultStatus.Updated, HandlerResult.Status);
    }

    [Fact]
    public void Deleted_ResultT()
    {
        var HandlerResult = ClientResult.Deleted().AsHandlerResult<NullPayload>();
        Assert.Equal(DefaultHandlerResultStatus.Deleted, HandlerResult.Status);
    }

    [Fact]
    public void NotFound_ResultT()
    {
        var HandlerResult = ClientResult.NotFound().AsHandlerResult<NullPayload>();
        Assert.Equal(DefaultHandlerResultStatus.NotFound, HandlerResult.Status);
    }

    [Fact]
    public void ServiceUnavailable_ResultT()
    {
        var HandlerResult = ClientResult.ServiceUnavailable().AsHandlerResult<NullPayload>();
        Assert.Equal(DefaultHandlerResultStatus.ServiceUnavailable, HandlerResult.Status);
    }
}
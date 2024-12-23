using Benzene.Results;
using Xunit;

namespace Benzene.Test.Core.Core.Results;

public class BenzeneResultTest
{
    [Fact]
    public void Accepted()
    {
        Assert.Equal(BenzeneResultStatus.Accepted, BenzeneResult.Accepted().Status);
    }

    [Fact]
    public void Accepted_T()
    {
        Assert.Equal(BenzeneResultStatus.Accepted, BenzeneResult.Accepted<Void>().Status);
    }

    [Fact]
    public void Success()
    {
        Assert.Equal(BenzeneResultStatus.Ok, BenzeneResult.Ok().Status);
    }

    [Fact]
    public void Success_T()
    {
        Assert.Equal(BenzeneResultStatus.Ok, BenzeneResult.Ok<Void>().Status);
    }

    [Fact]
    public void Created()
    {
        Assert.Equal(BenzeneResultStatus.Created, BenzeneResult.Created().Status);
    }

    [Fact]
    public void Created_T()
    {
        Assert.Equal(BenzeneResultStatus.Created, BenzeneResult.Created<Void>().Status);
    }

    [Fact]
    public void Updated()
    {
        Assert.Equal(BenzeneResultStatus.Updated, BenzeneResult.Updated().Status);
    }

    [Fact]
    public void Updated_T()
    {
        Assert.Equal(BenzeneResultStatus.Updated, BenzeneResult.Updated<Void>().Status);
    }

    [Fact]
    public void Deleted()
    {
        Assert.Equal(BenzeneResultStatus.Deleted, BenzeneResult.Deleted().Status);
    }

    [Fact]
    public void Deleted_T()
    {
        Assert.Equal(BenzeneResultStatus.Deleted, BenzeneResult.Deleted<Void>().Status);
    }

    [Fact]
    public void Ignored()
    {
        Assert.Equal(BenzeneResultStatus.Ignored, BenzeneResult.Ignored().Status);
    }

    [Fact]
    public void Ignored_T()
    {
        Assert.Equal(BenzeneResultStatus.Ignored, BenzeneResult.Ignored<Void>().Status);
    }

    [Fact]
    public void ValidationError()
    {
        Assert.Equal(BenzeneResultStatus.ValidationError, BenzeneResult.ValidationError().Status);
    }

    [Fact]
    public void ValidationError_T()
    {
        Assert.Equal(BenzeneResultStatus.ValidationError, BenzeneResult.ValidationError<Void>().Status);
    }

    [Fact]
    public void NotFound()
    {
        Assert.Equal(BenzeneResultStatus.NotFound, BenzeneResult.NotFound().Status);
    }

    [Fact]
    public void NotFound_T()
    {
        Assert.Equal(BenzeneResultStatus.NotFound, BenzeneResult.NotFound<Void>().Status);
    }

    [Fact]
    public void BadRequest()
    {
        Assert.Equal(BenzeneResultStatus.BadRequest, BenzeneResult.BadRequest().Status);
    }

    [Fact]
    public void BadRequest_T()
    {
        Assert.Equal(BenzeneResultStatus.BadRequest, BenzeneResult.BadRequest<Void>().Status);
    }

    [Fact]
    public void Forbidden()
    {
        Assert.Equal(BenzeneResultStatus.Forbidden, BenzeneResult.Forbidden().Status);
    }

    [Fact]
    public void Forbidden_T()
    {
        Assert.Equal(BenzeneResultStatus.Forbidden, BenzeneResult.Forbidden<Void>().Status);
    }

    [Fact]
    public void ServiceUnavailable()
    {
        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, BenzeneResult.ServiceUnavailable().Status);
    }

    [Fact]
    public void ServiceUnavailable_T()
    {
        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, BenzeneResult.ServiceUnavailable<Void>().Status);
    }

    [Fact]
    public void Conflict()
    {
        Assert.Equal(BenzeneResultStatus.Conflict, BenzeneResult.Conflict().Status);
    }

    [Fact]
    public void Conflict_T()
    {
        Assert.Equal(BenzeneResultStatus.Conflict, BenzeneResult.Conflict<Void>().Status);
    }

    [Fact]
    public void Unauthorized()
    {
        Assert.Equal(BenzeneResultStatus.Unauthorized, BenzeneResult.Unauthorized().Status);
    }

    [Fact]
    public void Unauthorized_T()
    {
        Assert.Equal(BenzeneResultStatus.Unauthorized, BenzeneResult.Unauthorized<Void>().Status);
    }

    [Fact]
    public void NotImplemented()
    {
        Assert.Equal(BenzeneResultStatus.NotImplemented, BenzeneResult.NotImplemented().Status);
    }

    [Fact]
    public void NotImplemented_T()
    {
        Assert.Equal(BenzeneResultStatus.NotImplemented, BenzeneResult.NotImplemented<Void>().Status);
    }
}

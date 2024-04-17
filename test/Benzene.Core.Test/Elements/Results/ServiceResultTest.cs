using Benzene.Core.Results;
using Benzene.Elements.Core.Results;
using Benzene.Results;
using Xunit;

namespace Benzene.Test.Elements.Results;

public class ClientResultTest
{
    [Fact]
    public void Accepted()
    {
        Assert.Equal(ClientResultStatus.Accepted, ClientResult.Accepted().Status);
    }

    [Fact]
    public void Accepted_T()
    {
        Assert.Equal(ClientResultStatus.Accepted, ClientResult.Accepted<Void>().Status);
    }

    [Fact]
    public void Success()
    {
        Assert.Equal(ClientResultStatus.Success, ClientResult.Success().Status);
    }

    [Fact]
    public void Success_T()
    {
        Assert.Equal(ClientResultStatus.Success, ClientResult.Success<Void>().Status);
    }

    [Fact]
    public void Ok()
    {
        Assert.Equal(ClientResultStatus.Ok, ClientResult.Ok().Status);
    }

    [Fact]
    public void Ok_T()
    {
        Assert.Equal(ClientResultStatus.Ok, ClientResult.Ok<Void>().Status);
    }


    [Fact]
    public void Created()
    {
        Assert.Equal(ClientResultStatus.Created, ClientResult.Created().Status);
    }

    [Fact]
    public void Created_T()
    {
        Assert.Equal(ClientResultStatus.Created, ClientResult.Created<Void>().Status);
    }

    [Fact]
    public void Updated()
    {
        Assert.Equal(ClientResultStatus.Updated, ClientResult.Updated().Status);
    }

    [Fact]
    public void Updated_T()
    {
        Assert.Equal(ClientResultStatus.Updated, ClientResult.Updated<Void>().Status);
    }

    [Fact]
    public void Deleted()
    {
        Assert.Equal(ClientResultStatus.Deleted, ClientResult.Deleted().Status);
    }

    [Fact]
    public void Deleted_T()
    {
        Assert.Equal(ClientResultStatus.Deleted, ClientResult.Deleted<Void>().Status);
    }

    [Fact]
    public void Ignored()
    {
        Assert.Equal(ClientResultStatus.Ignored, ClientResult.Ignored().Status);
    }

    [Fact]
    public void Ignored_T()
    {
        Assert.Equal(ClientResultStatus.Ignored, ClientResult.Ignored<Void>().Status);
    }

    [Fact]
    public void ValidationError()
    {
        Assert.Equal(ClientResultStatus.ValidationError, ClientResult.ValidationError().Status);
    }

    [Fact]
    public void ValidationError_T()
    {
        Assert.Equal(ClientResultStatus.ValidationError, ClientResult.ValidationError<Void>().Status);
    }

    [Fact]
    public void NotFound()
    {
        Assert.Equal(ClientResultStatus.NotFound, ClientResult.NotFound().Status);
    }

    [Fact]
    public void NotFound_T()
    {
        Assert.Equal(ClientResultStatus.NotFound, ClientResult.NotFound<Void>().Status);
    }

    [Fact]
    public void BadRequest()
    {
        Assert.Equal(ClientResultStatus.BadRequest, ClientResult.BadRequest().Status);
    }

    [Fact]
    public void BadRequest_T()
    {
        Assert.Equal(ClientResultStatus.BadRequest, ClientResult.BadRequest<Void>().Status);
    }

    [Fact]
    public void Forbidden()
    {
        Assert.Equal(ClientResultStatus.Forbidden, ClientResult.Forbidden().Status);
    }

    [Fact]
    public void Forbidden_T()
    {
        Assert.Equal(ClientResultStatus.Forbidden, ClientResult.Forbidden<Void>().Status);
    }

    [Fact]
    public void ServiceUnavailable()
    {
        Assert.Equal(ClientResultStatus.ServiceUnavailable, ClientResult.ServiceUnavailable().Status);
    }

    [Fact]
    public void ServiceUnavailable_T()
    {
        Assert.Equal(ClientResultStatus.ServiceUnavailable, ClientResult.ServiceUnavailable<Void>().Status);
    }

    [Fact]
    public void NotImplemented()
    {
        Assert.Equal(ClientResultStatus.NotImplemented, ClientResult.NotImplemented().Status);
    }

    [Fact]
    public void NotImplemented_T()
    {
        Assert.Equal(ClientResultStatus.NotImplemented, ClientResult.NotImplemented<Void>().Status);
    }

    [Fact]
    public void UnexpectedError()
    {
        Assert.Equal(ClientResultStatus.UnexpectedError, ClientResult.UnexpectedError().Status);
    }

    [Fact]
    public void UnexpectedError_T()
    {
        Assert.Equal(ClientResultStatus.UnexpectedError, ClientResult.UnexpectedError<Void>().Status);
    }

    [Fact]
    public void Conflict()
    {
        Assert.Equal(ClientResultStatus.Conflict, ClientResult.Conflict().Status);
    }

    [Fact]
    public void Conflict_T()
    {
        Assert.Equal(ClientResultStatus.Conflict, ClientResult.Conflict<Void>().Status);
    }

    [Fact]
    public void Unauthorized()
    {
        Assert.Equal(ClientResultStatus.Unauthorized, ClientResult.Unauthorized().Status);
    }

    [Fact]
    public void Unauthorized_T()
    {
        Assert.Equal(ClientResultStatus.Unauthorized, ClientResult.Unauthorized<Void>().Status);
    }
}

using Benzene.Results;
using Xunit;

namespace Benzene.Test.Core.Core.Results;

public class ServiceResultTest
{
    [Fact]
    public void Accepted()
    {
        Assert.Equal(ServiceResultStatus.Accepted, ServiceResult.Accepted().Status);
    }

    [Fact]
    public void Accepted_T()
    {
        Assert.Equal(ServiceResultStatus.Accepted, ServiceResult.Accepted<Void>().Status);
    }

    [Fact]
    public void Success()
    {
        Assert.Equal(ServiceResultStatus.Ok, ServiceResult.Ok().Status);
    }

    [Fact]
    public void Success_T()
    {
        Assert.Equal(ServiceResultStatus.Ok, ServiceResult.Ok<Void>().Status);
    }

    [Fact]
    public void Created()
    {
        Assert.Equal(ServiceResultStatus.Created, ServiceResult.Created().Status);
    }

    [Fact]
    public void Created_T()
    {
        Assert.Equal(ServiceResultStatus.Created, ServiceResult.Created<Void>().Status);
    }

    [Fact]
    public void Updated()
    {
        Assert.Equal(ServiceResultStatus.Updated, ServiceResult.Updated().Status);
    }

    [Fact]
    public void Updated_T()
    {
        Assert.Equal(ServiceResultStatus.Updated, ServiceResult.Updated<Void>().Status);
    }

    [Fact]
    public void Deleted()
    {
        Assert.Equal(ServiceResultStatus.Deleted, ServiceResult.Deleted().Status);
    }

    [Fact]
    public void Deleted_T()
    {
        Assert.Equal(ServiceResultStatus.Deleted, ServiceResult.Deleted<Void>().Status);
    }

    [Fact]
    public void Ignored()
    {
        Assert.Equal(ServiceResultStatus.Ignored, ServiceResult.Ignored().Status);
    }

    [Fact]
    public void Ignored_T()
    {
        Assert.Equal(ServiceResultStatus.Ignored, ServiceResult.Ignored<Void>().Status);
    }

    [Fact]
    public void ValidationError()
    {
        Assert.Equal(ServiceResultStatus.ValidationError, ServiceResult.ValidationError().Status);
    }

    [Fact]
    public void ValidationError_T()
    {
        Assert.Equal(ServiceResultStatus.ValidationError, ServiceResult.ValidationError<Void>().Status);
    }

    [Fact]
    public void NotFound()
    {
        Assert.Equal(ServiceResultStatus.NotFound, ServiceResult.NotFound().Status);
    }

    [Fact]
    public void NotFound_T()
    {
        Assert.Equal(ServiceResultStatus.NotFound, ServiceResult.NotFound<Void>().Status);
    }

    [Fact]
    public void BadRequest()
    {
        Assert.Equal(ServiceResultStatus.BadRequest, ServiceResult.BadRequest().Status);
    }

    [Fact]
    public void BadRequest_T()
    {
        Assert.Equal(ServiceResultStatus.BadRequest, ServiceResult.BadRequest<Void>().Status);
    }

    [Fact]
    public void Forbidden()
    {
        Assert.Equal(ServiceResultStatus.Forbidden, ServiceResult.Forbidden().Status);
    }

    [Fact]
    public void Forbidden_T()
    {
        Assert.Equal(ServiceResultStatus.Forbidden, ServiceResult.Forbidden<Void>().Status);
    }

    [Fact]
    public void ServiceUnavailable()
    {
        Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.ServiceUnavailable().Status);
    }

    [Fact]
    public void ServiceUnavailable_T()
    {
        Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.ServiceUnavailable<Void>().Status);
    }

    [Fact]
    public void Conflict()
    {
        Assert.Equal(ServiceResultStatus.Conflict, ServiceResult.Conflict().Status);
    }

    [Fact]
    public void Conflict_T()
    {
        Assert.Equal(ServiceResultStatus.Conflict, ServiceResult.Conflict<Void>().Status);
    }

    [Fact]
    public void Unauthorized()
    {
        Assert.Equal(ServiceResultStatus.Unauthorized, ServiceResult.Unauthorized().Status);
    }

    [Fact]
    public void Unauthorized_T()
    {
        Assert.Equal(ServiceResultStatus.Unauthorized, ServiceResult.Unauthorized<Void>().Status);
    }

    [Fact]
    public void NotImplemented()
    {
        Assert.Equal(ServiceResultStatus.NotImplemented, ServiceResult.NotImplemented().Status);
    }

    [Fact]
    public void NotImplemented_T()
    {
        Assert.Equal(ServiceResultStatus.NotImplemented, ServiceResult.NotImplemented<Void>().Status);
    }
}

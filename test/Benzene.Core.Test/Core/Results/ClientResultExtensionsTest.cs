using System.Threading.Tasks;
using Benzene.Results;
using Benzene.Test.Clients.Samples;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Core.Results;

public class ClientResultExtensionsTest
{
    [Fact]
    public void IsSuccess_True()
    {
        Assert.True(ClientResult.Success().IsSuccess());
    }
    [Fact]
    public void IsSuccess_False()
    {
        Assert.False(ClientResult.UnexpectedError().IsSuccess());
    }

    [Fact]
    public void IsAccepted_True()
    {
        Assert.True(ClientResult.Accepted().IsAccepted());
    }
    [Fact]
    public void IsAccepted_False()
    {
        Assert.False(ClientResult.UnexpectedError().IsAccepted());
    }
    [Fact]
    public void IsCreated_True()
    {
        Assert.True(ClientResult.Created().IsCreated());
    }
    [Fact]
    public void IsCreated_False()
    {
        Assert.False(ClientResult.UnexpectedError().IsCreated());
    }
    [Fact]
    public void IsUpdated_True()
    {
        Assert.True(ClientResult.Updated().IsUpdated());
    }
    [Fact]
    public void IsUpdated_False()
    {
        Assert.False(ClientResult.UnexpectedError().IsUpdated());
    }
    [Fact]
    public void IsDeleted_True()
    {
        Assert.True(ClientResult.Deleted().IsDeleted());
    }
    [Fact]
    public void IsDeleted_False()
    {
        Assert.False(ClientResult.UnexpectedError().IsDeleted());
    }
    [Fact]
    public void IsIgnored_True()
    {
        Assert.True(ClientResult.Ignored().IsIgnored());
    }
    [Fact]
    public void IsIgnored_False()
    {
        Assert.False(ClientResult.UnexpectedError().IsIgnored());
    }

    [Fact]
    public void IsNotFound_True()
    {
        Assert.True(ClientResult.NotFound().IsNotFound());
    }
    [Fact]
    public void IsNotFound_False()
    {
        Assert.False(ClientResult.UnexpectedError().IsNotFound());
    }
    [Fact]
    public void IsBadRequest_True()
    {
        Assert.True(ClientResult.BadRequest().IsBadRequest());
    }
    [Fact]
    public void IsBadRequest_False()
    {
        Assert.False(ClientResult.UnexpectedError().IsBadRequest());
    }
    [Fact]
    public void IsValidationError_True()
    {
        Assert.True(ClientResult.ValidationError().IsValidationError());
    }
    [Fact]
    public void IsValidationError_False()
    {
        Assert.False(ClientResult.UnexpectedError().IsValidationError());
    }
    [Fact]
    public void IsServiceUnavailable_True()
    {
        Assert.True(ClientResult.ServiceUnavailable().IsServiceUnavailable());
    }
    [Fact]
    public void IsServiceUnavailable_False()
    {
        Assert.False(ClientResult.UnexpectedError().IsServiceUnavailable());
    }
    [Fact]
    public void IsNotImplemented_True()
    {
        Assert.True(ClientResult.NotImplemented().IsNotImplemented());
    }
    [Fact]
    public void IsNotImplemented_False()
    {
        Assert.False(ClientResult.UnexpectedError().IsNotImplemented());
    }
    [Fact]
    public void IsUnexpectedError_True()
    {
        Assert.True(ClientResult.UnexpectedError().IsUnexpectedError());
    }
    [Fact]
    public void IsUnexpectedError_False()
    {
        Assert.False(ClientResult.Success().IsUnexpectedError());
    }
    [Fact]
    public void IsConflict_True()
    {
        Assert.True(ClientResult.Conflict().IsConflict());
    }
    [Fact]
    public void IsConflict_False()
    {
        Assert.False(ClientResult.UnexpectedError().IsConflict());
    }
    [Fact]
    public void IsForbidden_True()
    {
        Assert.True(ClientResult.Forbidden().IsForbidden());
    }
    [Fact]
    public void IsForbidden_False()
    {
        Assert.False(ClientResult.UnexpectedError().IsForbidden());
    }
    [Fact]
    public void IsUnauthorized_True()
    {
        Assert.True(ClientResult.Unauthorized().IsUnauthorized());
    }
    [Fact]
    public void IsUnauthorized_False()
    {
        Assert.False(ClientResult.UnexpectedError().IsUnauthorized());
    }

    [Fact]
    public void AsServiceResult_T()
    {
        var result = ClientResult.Ok(new ExamplePayload()).AsServiceResult();
        Assert.Equal(ClientResultStatus.Ok, result.Status);
    }

    [Fact]
    public void AsServiceResult()
    {
        var result = ClientResult.Accepted().AsServiceResult();
        Assert.Equal(ClientResultStatus.Accepted, result.Status);
    }

    [Fact]
    public void AsServiceResultTask_T()
    {
        var result = Task.FromResult(ClientResult.Accepted(new ExamplePayload())).AsServiceResult().Result;
        Assert.Equal(ClientResultStatus.Accepted, result.Status);
    }

    [Fact]
    public void AsServiceResultTask()
    {
        var result = Task.FromResult(ClientResult.Accepted()).AsServiceResult().Result;
        Assert.Equal(ClientResultStatus.Accepted, result.Status);
    }

    [Fact]
    public void AsServiceResult_T2()
    {
        var result = ClientResult.Accepted().AsServiceResult<ExamplePayload>();
        Assert.Equal(ClientResultStatus.Accepted, result.Status);
    }

    [Fact]
    public void As()
    {
        var result = ClientResult.Created().As<ExampleResponsePayload>();
        Assert.Equal(ClientResultStatus.Created, result.Status);
    }

    [Fact]
    public void As2()
    {
        var exampleResponsePayload = new ExampleResponsePayload { Name = "foo" };
        var result = ClientResult.Created(new ExamplePayload()).As(exampleResponsePayload);
        Assert.Equal(ClientResultStatus.Created, result.Status);
        Assert.Equal("foo", result.Payload.Name);
    }

    [Fact]
    public void MapIfSuccessful()
    {
        var result = Task.FromResult(ClientResult.Success(new ExamplePayload { Name = "foo" }))
            .MapIfSuccessful(x => new ExampleResponsePayload { Name = x.Name })
            .Result;
        Assert.Equal(ClientResultStatus.Ok, result.Status);
        Assert.Equal("foo", result.Payload.Name);
    }

    [Fact]
    public void MapIfSuccessful_Errors()
    {
        var result = Task.FromResult(ClientResult.BadRequest<ExamplePayload>("foo"))
            .MapIfSuccessful(x => new ExampleResponsePayload { Name = x.Name })
            .Result;
        Assert.Equal(ClientResultStatus.BadRequest, result.Status);
        Assert.Equal("foo", result.Errors[0]);
    }

    [Fact]
    public void AsServiceResultMapIfSuccessful()
    {
        var result = Task.FromResult(ClientResult.Success(new ExamplePayload { Name = "foo" }))
            .AsServiceResultMapIfSuccessful(x => new ExampleResponsePayload { Name = x.Name })
            .Result;
        Assert.Equal(ClientResultStatus.Ok, result.Status);
        Assert.Equal("foo", result.Payload.Name);
    }
}

using System.Threading.Tasks;
using Benzene.Http;
using Benzene.Results;
using Xunit;

namespace Benzene.Test.Core.Results;

public class ServiceResultExtensionsTest
{
    [Fact]
    public void IsOk_True()
    {
        Assert.True(ServiceResult.Ok().IsOk());
    }
    [Fact]
    public void IsOk_False()
    {
        Assert.False(ServiceResult.UnexpectedError().IsOk());
    }
    [Fact]
    public void IsSuccess_False()
    {
        Assert.False(ServiceResult.UnexpectedError().IsSuccess());
    }
    [Fact]
    public void IsAccepted_True()
    {
        Assert.True(ServiceResult.Accepted().IsAccepted());
    }
    [Fact]
    public void IsAccepted_False()
    {
        Assert.False(ServiceResult.UnexpectedError().IsAccepted());
    }
    [Fact]
    public void IsCreated_True()
    {
        Assert.True(ServiceResult.Created().IsCreated());
    }
    [Fact]
    public void IsCreated_False()
    {
        Assert.False(ServiceResult.UnexpectedError().IsCreated());
    }
    [Fact]
    public void IsUpdated_True()
    {
        Assert.True(ServiceResult.Updated().IsUpdated());
    }
    [Fact]
    public void IsUpdated_False()
    {
        Assert.False(ServiceResult.UnexpectedError().IsUpdated());
    }
    [Fact]
    public void IsDeleted_True()
    {
        Assert.True(ServiceResult.Deleted().IsDeleted());
    }
    [Fact]
    public void IsDeleted_False()
    {
        Assert.False(ServiceResult.UnexpectedError().IsDeleted());
    }
    [Fact]
    public void IsIgnored_True()
    {
        Assert.True(ServiceResult.Ignored().IsIgnored());
    }
    [Fact]
    public void IsIgnored_False()
    {
        Assert.False(ServiceResult.UnexpectedError().IsIgnored());
    }
    [Fact]
    public void IsNotFound_True()
    {
        Assert.True(ServiceResult.NotFound().IsNotFound());
    }
    [Fact]
    public void IsNotFound_False()
    {
        Assert.False(ServiceResult.UnexpectedError().IsNotFound());
    }
    [Fact]
    public void IsBadRequest_True()
    {
        Assert.True(ServiceResult.BadRequest().IsBadRequest());
    }
    [Fact]
    public void IsBadRequest_False()
    {
        Assert.False(ServiceResult.UnexpectedError().IsBadRequest());
    }
    [Fact]
    public void IsValidationError_True()
    {
        Assert.True(ServiceResult.ValidationError().IsValidationError());
    }
    [Fact]
    public void IsValidationError_False()
    {
        Assert.False(ServiceResult.UnexpectedError().IsValidationError());
    }
    [Fact]
    public void IsServiceUnavailable_True()
    {
        Assert.True(ServiceResult.ServiceUnavailable().IsServiceUnavailable());
    }
    [Fact]
    public void IsServiceUnavailable_False()
    {
        Assert.False(ServiceResult.UnexpectedError().IsServiceUnavailable());
    }
    [Fact]
    public void IsNotImplemented_True()
    {
        Assert.True(ServiceResult.NotImplemented().IsNotImplemented());
    }
    [Fact]
    public void IsNotImplemented_False()
    {
        Assert.False(ServiceResult.UnexpectedError().IsNotImplemented());
    }
    [Fact]
    public void IsUnexpectedError_True()
    {
        Assert.True(ServiceResult.UnexpectedError().IsUnexpectedError());
    }
    [Fact]
    public void IsUnexpectedError_False()
    {
        Assert.False(ServiceResult.Ok().IsUnexpectedError());
    }
    [Fact]
    public void IsConflict_True()
    {
        Assert.True(ServiceResult.Conflict().IsConflict());
    }
    [Fact]
    public void IsConflict_False()
    {
        Assert.False(ServiceResult.UnexpectedError().IsConflict());
    }
    [Fact]
    public void IsForbidden_True()
    {
        Assert.True(ServiceResult.Forbidden().IsForbidden());
    }
    [Fact]
    public void IsForbidden_False()
    {
        Assert.False(ServiceResult.UnexpectedError().IsForbidden());
    }
    [Fact]
    public void IsUnauthorized_True()
    {
        Assert.True(ServiceResult.Unauthorized().IsUnauthorized());
    }
    [Fact]
    public void IsUnauthorized_False()
    {
        Assert.False(ServiceResult.UnexpectedError().IsUnauthorized());
    }

    [Fact]
    public void AsPayload()
    {
        Assert.Equal(ServiceResultStatus.Accepted, ServiceResult.Accepted().As<Void>().Status);
        Assert.Equal(ServiceResultStatus.Ok, ServiceResult.Ok().As<Void>().Status);
        Assert.Equal(ServiceResultStatus.Created, ServiceResult.Created().As<Void>().Status);
        Assert.Equal(ServiceResultStatus.Updated, ServiceResult.Updated().As<Void>().Status);
        Assert.Equal(ServiceResultStatus.Deleted, ServiceResult.Deleted().As<Void>().Status);
        Assert.Equal(ServiceResultStatus.ValidationError, ServiceResult.ValidationError().As<Void>().Status);
        Assert.Equal(ServiceResultStatus.NotFound, ServiceResult.NotFound().As<Void>().Status);
        Assert.Equal(ServiceResultStatus.Forbidden, ServiceResult.Forbidden().As<Void>().Status);
        Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.ServiceUnavailable().As<Void>().Status);
        Assert.Equal(ServiceResultStatus.Conflict, ServiceResult.Conflict().As<Void>().Status);
        Assert.Equal(ServiceResultStatus.Unauthorized, ServiceResult.Unauthorized().As<Void>().Status);
    }

    [Fact]
    public void AsPayload_Map()
    {
        Assert.Equal(ServiceResultStatus.Accepted, ServiceResult.Accepted<Void>().As(_ => new Void()).Status);
        Assert.Equal(ServiceResultStatus.Ok, ServiceResult.Ok<Void>().As(_ => new Void()).Status);
        Assert.Equal(ServiceResultStatus.Created, ServiceResult.Created<Void>().As(_ => new Void()).Status);
        Assert.Equal(ServiceResultStatus.Updated, ServiceResult.Updated<Void>().As(_ => new Void()).Status);
        Assert.Equal(ServiceResultStatus.Deleted, ServiceResult.Deleted<Void>().As(_ => new Void()).Status);
        Assert.Equal(ServiceResultStatus.ValidationError, ServiceResult.ValidationError<Void>().As(_ => new Void()).Status);
        Assert.Equal(ServiceResultStatus.NotFound, ServiceResult.NotFound<Void>().As(_ => new Void()).Status);
        Assert.Equal(ServiceResultStatus.Forbidden, ServiceResult.Forbidden<Void>().As(_ => new Void()).Status);
        Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.ServiceUnavailable<Void>().As(_ => new Void()).Status);
        Assert.Equal(ServiceResultStatus.Conflict, ServiceResult.Conflict<Void>().As(_ => new Void()).Status);
        Assert.Equal(ServiceResultStatus.Unauthorized, ServiceResult.Unauthorized<Void>().As(_ => new Void()).Status);
    }


    [Fact]
    public void AsPayload_Task()
    {
        Assert.Equal(ServiceResultStatus.Ok, Task.FromResult(ServiceResult.Ok()).As<Void>().Result.Status);
    }

    [Fact]
    public void AsPayload_IServiceResult()
    {
        Assert.Equal(ServiceResultStatus.Ok, (ServiceResult.Ok() as IServiceResult<Void>).As<Void>().Status);
    }

    [Fact]
    public void AsPayload_IServiceResult_Task()
    {
        Assert.Equal(ServiceResultStatus.Ok, Task.FromResult(ServiceResult.Ok()).As(new Void()).Result.Status);
    }

    [Fact]
    public void AsPayload_IServiceResult_T_Task()
    {
        Assert.Equal(ServiceResultStatus.Ok, Task.FromResult(ServiceResult.Ok() as IServiceResult<Void>).As(_ => new Void()).Result.Status);
    }

    [Fact]
    public void AsPayload_IServiceResult_T_Map()
    {
        Assert.Equal(ServiceResultStatus.Ok, (ServiceResult.Ok() as IServiceResult<Void>).As<Void, Void>().Status);
    }

    [Fact]
    public void AsPayload_IServiceResult_T_Map_Task()
    {
        Assert.Equal(ServiceResultStatus.Ok, Task.FromResult(ServiceResult.Ok() as IServiceResult<Void>).As<Void, Void>().Result.Status);
    }

    [Theory]
    [InlineData(ServiceResultStatus.Accepted, 202)]
    [InlineData(ServiceResultStatus.Ok, 200)]
    [InlineData(ServiceResultStatus.Created, 201)]
    [InlineData(ServiceResultStatus.Updated, 204)]
    [InlineData(ServiceResultStatus.Deleted, 204)]
    [InlineData(ServiceResultStatus.Forbidden, 403)]
    [InlineData(ServiceResultStatus.NotFound, 404)]
    [InlineData(ServiceResultStatus.Conflict, 409)]
    [InlineData(ServiceResultStatus.ValidationError, 422)]
    [InlineData(ServiceResultStatus.NotImplemented, 501)]
    [InlineData(ServiceResultStatus.ServiceUnavailable, 503)]
    [InlineData(null, 500)]
    public void AsHttpStatusCode(string status, int expectedStatusCode)
    {
        var httpStatusCodeMapper = new DefaultHttpStatusCodeMapper();
        Assert.Equal(expectedStatusCode.ToString(), httpStatusCodeMapper.Map(status));
    }

    // [Theory]
    // [InlineData(ServiceResultStatus.Accepted, true)]
    // [InlineData(ServiceResultStatus.Ok, true)]
    // [InlineData(ServiceResultStatus.Created, true)]
    // [InlineData(ServiceResultStatus.Updated,true)]
    // [InlineData(ServiceResultStatus.Deleted, true)]
    // [InlineData(ServiceResultStatus.Forbidden, false)]
    // [InlineData(ServiceResultStatus.NotFound, false)]
    // [InlineData(ServiceResultStatus.Conflict, false)]
    // [InlineData(ServiceResultStatus.ValidationError, false)]
    // [InlineData(ServiceResultStatus.NotImplemented, false)]
    // [InlineData(ServiceResultStatus.ServiceUnavailable, false)]
    // [InlineData(null, false)]
    // public void IsSuccessful(string status, bool expected)
    // {
    //     Assert.Equal(expected, status.IsSuccessful());
    // }

    // [Fact]
    // public void AsServiceResult_IServiceResult()
    // {
    //     Assert.Equal(ServiceResultStatus.Accepted, (ServiceResult.Accepted() as IServiceResult<NullPayload>).AsServiceResult().Status);
    // }
    //
    // [Fact]
    // public void AsServiceResult_IServiceResult_Task()
    // {
    //     Assert.Equal(ServiceResultStatus.Accepted, Task.FromResult(ServiceResult.Accepted() as IServiceResult<NullPayload>).AsServiceResult().Result.Status);
    // }
    //
    // [Fact]
    // public void AsServiceResult_T()
    // {
    //     Assert.Equal(ServiceResultStatus.Accepted, ServiceResult.Accepted().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.Ok, ServiceResult.Ok().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.Created, ServiceResult.Created().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.Updated, ServiceResult.Updated().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.Deleted, ServiceResult.Deleted().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.ValidationError<NullPayload>().AsType<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.NotFound, ServiceResult.NotFound().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.Forbidden().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.ServiceUnavailable().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.Conflict, ServiceResult.Conflict().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.Unauthorized().AsServiceResult<NullPayload>().Status);
    // }
    //
    // [Fact]
    // public void AsServiceResult_Payload()
    // {
    //     Assert.Equal(ServiceResultStatus.Accepted, ServiceResult.Accepted<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.Ok, ServiceResult.Ok<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.Created, ServiceResult.Created<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.Updated, ServiceResult.Updated<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.Deleted, ServiceResult.Deleted<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable,
    //         ServiceResult.ValidationError<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.NotFound, ServiceResult.NotFound<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.Forbidden<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable,
    //         ServiceResult.ServiceUnavailable<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.Conflict, ServiceResult.Conflict<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.Unauthorized<NullPayload>().AsServiceResult(new NullPayload()).Status);
    // }
    //
    // [Fact]
    // public void AsServiceResult_Task()
    // {
    //     Assert.Equal(ServiceResultStatus.Ok, Task.FromResult(ServiceResult.Ok().AsServiceResult()).Result.Status);
    // }
    //
    // [Fact]
    // public void AsType_T()
    // {
    //     Assert.Equal(ServiceResultStatus.Accepted, ServiceResult.Accepted().AsType<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.Ok, ServiceResult.Ok().AsType<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.Created, ServiceResult.Created().AsType<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.Updated, ServiceResult.Updated().AsType<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.Deleted, ServiceResult.Deleted().AsType<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.ValidationError().AsType<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.NotFound, ServiceResult.NotFound().AsType<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.Forbidden().AsType<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.ServiceUnavailable().AsType<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.Conflict, ServiceResult.Conflict().AsType<NullPayload>().Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.Unauthorized().AsType<NullPayload>().Status);
    // }
    //
    // [Fact]
    // public void AsType()
    // {
    //     Assert.Equal(ServiceResultStatus.Accepted, ServiceResult.Accepted<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.Ok, ServiceResult.Ok<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.Created, ServiceResult.Created<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.Updated, ServiceResult.Updated<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.Deleted, ServiceResult.Deleted<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.ValidationError<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.NotFound, ServiceResult.NotFound<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.Forbidden<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.ServiceUnavailable<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.Conflict, ServiceResult.Conflict<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(ServiceResultStatus.ServiceUnavailable, ServiceResult.Unauthorized<NullPayload>().AsType(new NullPayload()).Status);
    // }
    //
    // [Fact]
    // public void MapIfSuccessful()
    // {
    //     Assert.Equal(ServiceResultStatus.Ok, Task.FromResult(ServiceResult.Ok<NullPayload>()).MapIfSuccessful(x => x).Result.Status);
    // }
    //
    // [Fact]
    // public void AsServiceResultMapIfSuccessful()
    // {
    //     Assert.Equal(ServiceResultStatus.Ok, Task.FromResult(ServiceResult.Ok<NullPayload>()).AsServiceResultMapIfSuccessful(x => x).Result.Status);
    // }
}

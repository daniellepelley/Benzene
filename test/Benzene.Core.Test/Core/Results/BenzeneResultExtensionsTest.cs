using System.Threading.Tasks;
using Benzene.Http;
using Benzene.Results;
using Xunit;

namespace Benzene.Test.Core.Results;

public class BenzeneResultExtensionsTest
{
    [Fact]
    public void IsOk_True()
    {
        Assert.True(BenzeneResult.Ok().IsOk());
    }
    [Fact]
    public void IsOk_False()
    {
        Assert.False(BenzeneResult.UnexpectedError().IsOk());
    }
    [Fact]
    public void IsSuccess_False()
    {
        Assert.False(BenzeneResult.UnexpectedError().IsSuccess());
    }
    [Fact]
    public void IsAccepted_True()
    {
        Assert.True(BenzeneResult.Accepted().IsAccepted());
    }
    [Fact]
    public void IsAccepted_False()
    {
        Assert.False(BenzeneResult.UnexpectedError().IsAccepted());
    }
    [Fact]
    public void IsCreated_True()
    {
        Assert.True(BenzeneResult.Created().IsCreated());
    }
    [Fact]
    public void IsCreated_False()
    {
        Assert.False(BenzeneResult.UnexpectedError().IsCreated());
    }
    [Fact]
    public void IsUpdated_True()
    {
        Assert.True(BenzeneResult.Updated().IsUpdated());
    }
    [Fact]
    public void IsUpdated_False()
    {
        Assert.False(BenzeneResult.UnexpectedError().IsUpdated());
    }
    [Fact]
    public void IsDeleted_True()
    {
        Assert.True(BenzeneResult.Deleted().IsDeleted());
    }
    [Fact]
    public void IsDeleted_False()
    {
        Assert.False(BenzeneResult.UnexpectedError().IsDeleted());
    }
    [Fact]
    public void IsIgnored_True()
    {
        Assert.True(BenzeneResult.Ignored().IsIgnored());
    }
    [Fact]
    public void IsIgnored_False()
    {
        Assert.False(BenzeneResult.UnexpectedError().IsIgnored());
    }
    [Fact]
    public void IsNotFound_True()
    {
        Assert.True(BenzeneResult.NotFound().IsNotFound());
    }
    [Fact]
    public void IsNotFound_False()
    {
        Assert.False(BenzeneResult.UnexpectedError().IsNotFound());
    }
    [Fact]
    public void IsBadRequest_True()
    {
        Assert.True(BenzeneResult.BadRequest().IsBadRequest());
    }
    [Fact]
    public void IsBadRequest_False()
    {
        Assert.False(BenzeneResult.UnexpectedError().IsBadRequest());
    }
    [Fact]
    public void IsValidationError_True()
    {
        Assert.True(BenzeneResult.ValidationError().IsValidationError());
    }
    [Fact]
    public void IsValidationError_False()
    {
        Assert.False(BenzeneResult.UnexpectedError().IsValidationError());
    }
    [Fact]
    public void IsServiceUnavailable_True()
    {
        Assert.True(BenzeneResult.ServiceUnavailable().IsServiceUnavailable());
    }
    [Fact]
    public void IsServiceUnavailable_False()
    {
        Assert.False(BenzeneResult.UnexpectedError().IsServiceUnavailable());
    }
    [Fact]
    public void IsNotImplemented_True()
    {
        Assert.True(BenzeneResult.NotImplemented().IsNotImplemented());
    }
    [Fact]
    public void IsNotImplemented_False()
    {
        Assert.False(BenzeneResult.UnexpectedError().IsNotImplemented());
    }
    [Fact]
    public void IsUnexpectedError_True()
    {
        Assert.True(BenzeneResult.UnexpectedError().IsUnexpectedError());
    }
    [Fact]
    public void IsUnexpectedError_False()
    {
        Assert.False(BenzeneResult.Ok().IsUnexpectedError());
    }
    [Fact]
    public void IsConflict_True()
    {
        Assert.True(BenzeneResult.Conflict().IsConflict());
    }
    [Fact]
    public void IsConflict_False()
    {
        Assert.False(BenzeneResult.UnexpectedError().IsConflict());
    }
    [Fact]
    public void IsForbidden_True()
    {
        Assert.True(BenzeneResult.Forbidden().IsForbidden());
    }
    [Fact]
    public void IsForbidden_False()
    {
        Assert.False(BenzeneResult.UnexpectedError().IsForbidden());
    }
    [Fact]
    public void IsUnauthorized_True()
    {
        Assert.True(BenzeneResult.Unauthorized().IsUnauthorized());
    }
    [Fact]
    public void IsUnauthorized_False()
    {
        Assert.False(BenzeneResult.UnexpectedError().IsUnauthorized());
    }

    [Fact]
    public void AsPayload()
    {
        Assert.Equal(BenzeneResultStatus.Accepted, BenzeneResult.Accepted().As<Void>().Status);
        Assert.Equal(BenzeneResultStatus.Ok, BenzeneResult.Ok().As<Void>().Status);
        Assert.Equal(BenzeneResultStatus.Created, BenzeneResult.Created().As<Void>().Status);
        Assert.Equal(BenzeneResultStatus.Updated, BenzeneResult.Updated().As<Void>().Status);
        Assert.Equal(BenzeneResultStatus.Deleted, BenzeneResult.Deleted().As<Void>().Status);
        Assert.Equal(BenzeneResultStatus.ValidationError, BenzeneResult.ValidationError().As<Void>().Status);
        Assert.Equal(BenzeneResultStatus.NotFound, BenzeneResult.NotFound().As<Void>().Status);
        Assert.Equal(BenzeneResultStatus.Forbidden, BenzeneResult.Forbidden().As<Void>().Status);
        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, BenzeneResult.ServiceUnavailable().As<Void>().Status);
        Assert.Equal(BenzeneResultStatus.Conflict, BenzeneResult.Conflict().As<Void>().Status);
        Assert.Equal(BenzeneResultStatus.Unauthorized, BenzeneResult.Unauthorized().As<Void>().Status);
    }

    [Fact]
    public void AsPayload_Map()
    {
        Assert.Equal(BenzeneResultStatus.Accepted, BenzeneResult.Accepted<Void>().As(_ => new Void()).Status);
        Assert.Equal(BenzeneResultStatus.Ok, BenzeneResult.Ok<Void>().As(_ => new Void()).Status);
        Assert.Equal(BenzeneResultStatus.Created, BenzeneResult.Created<Void>().As(_ => new Void()).Status);
        Assert.Equal(BenzeneResultStatus.Updated, BenzeneResult.Updated<Void>().As(_ => new Void()).Status);
        Assert.Equal(BenzeneResultStatus.Deleted, BenzeneResult.Deleted<Void>().As(_ => new Void()).Status);
        Assert.Equal(BenzeneResultStatus.ValidationError, BenzeneResult.ValidationError<Void>().As(_ => new Void()).Status);
        Assert.Equal(BenzeneResultStatus.NotFound, BenzeneResult.NotFound<Void>().As(_ => new Void()).Status);
        Assert.Equal(BenzeneResultStatus.Forbidden, BenzeneResult.Forbidden<Void>().As(_ => new Void()).Status);
        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, BenzeneResult.ServiceUnavailable<Void>().As(_ => new Void()).Status);
        Assert.Equal(BenzeneResultStatus.Conflict, BenzeneResult.Conflict<Void>().As(_ => new Void()).Status);
        Assert.Equal(BenzeneResultStatus.Unauthorized, BenzeneResult.Unauthorized<Void>().As(_ => new Void()).Status);
    }


    [Fact]
    public void AsPayload_Task()
    {
        Assert.Equal(BenzeneResultStatus.Ok, Task.FromResult(BenzeneResult.Ok()).As<Void>().Result.Status);
    }

    [Fact]
    public void AsPayload_IBenzeneResult()
    {
        Assert.Equal(BenzeneResultStatus.Ok, (BenzeneResult.Ok() as IBenzeneResult<Void>).As<Void>().Status);
    }

    [Fact]
    public void AsPayload_IBenzeneResult_Task()
    {
        Assert.Equal(BenzeneResultStatus.Ok, Task.FromResult(BenzeneResult.Ok()).As(new Void()).Result.Status);
    }

    [Fact]
    public void AsPayload_IBenzeneResult_T_Task()
    {
        Assert.Equal(BenzeneResultStatus.Ok, Task.FromResult(BenzeneResult.Ok() as IBenzeneResult<Void>).As(_ => new Void()).Result.Status);
    }

    [Fact]
    public void AsPayload_IBenzeneResult_T_Map()
    {
        Assert.Equal(BenzeneResultStatus.Ok, (BenzeneResult.Ok() as IBenzeneResult<Void>).As<Void, Void>().Status);
    }

    [Fact]
    public void AsPayload_IBenzeneResult_T_Map_Task()
    {
        Assert.Equal(BenzeneResultStatus.Ok, Task.FromResult(BenzeneResult.Ok() as IBenzeneResult<Void>).As<Void, Void>().Result.Status);
    }

    [Theory]
    [InlineData(BenzeneResultStatus.Accepted, 202)]
    [InlineData(BenzeneResultStatus.Ok, 200)]
    [InlineData(BenzeneResultStatus.Created, 201)]
    [InlineData(BenzeneResultStatus.Updated, 204)]
    [InlineData(BenzeneResultStatus.Deleted, 204)]
    [InlineData(BenzeneResultStatus.Forbidden, 403)]
    [InlineData(BenzeneResultStatus.NotFound, 404)]
    [InlineData(BenzeneResultStatus.Conflict, 409)]
    [InlineData(BenzeneResultStatus.ValidationError, 422)]
    [InlineData(BenzeneResultStatus.NotImplemented, 501)]
    [InlineData(BenzeneResultStatus.ServiceUnavailable, 503)]
    [InlineData(null, 500)]
    public void AsHttpStatusCode(string status, int expectedStatusCode)
    {
        var httpStatusCodeMapper = new DefaultHttpStatusCodeMapper();
        Assert.Equal(expectedStatusCode.ToString(), httpStatusCodeMapper.Map(status));
    }

    // [Theory]
    // [InlineData(BenzeneResultStatus.Accepted, true)]
    // [InlineData(BenzeneResultStatus.Ok, true)]
    // [InlineData(BenzeneResultStatus.Created, true)]
    // [InlineData(BenzeneResultStatus.Updated,true)]
    // [InlineData(BenzeneResultStatus.Deleted, true)]
    // [InlineData(BenzeneResultStatus.Forbidden, false)]
    // [InlineData(BenzeneResultStatus.NotFound, false)]
    // [InlineData(BenzeneResultStatus.Conflict, false)]
    // [InlineData(BenzeneResultStatus.ValidationError, false)]
    // [InlineData(BenzeneResultStatus.NotImplemented, false)]
    // [InlineData(BenzeneResultStatus.ServiceUnavailable, false)]
    // [InlineData(null, false)]
    // public void IsSuccessful(string status, bool expected)
    // {
    //     Assert.Equal(expected, status.IsSuccessful());
    // }

    // [Fact]
    // public void AsServiceResult_IBenzeneResult()
    // {
    //     Assert.Equal(BenzeneResultStatus.Accepted, (ServiceResult.Accepted() as IBenzeneResult<NullPayload>).AsServiceResult().Status);
    // }
    //
    // [Fact]
    // public void AsServiceResult_IBenzeneResult_Task()
    // {
    //     Assert.Equal(BenzeneResultStatus.Accepted, Task.FromResult(ServiceResult.Accepted() as IBenzeneResult<NullPayload>).AsServiceResult().BenzeneResult.Status);
    // }
    //
    // [Fact]
    // public void AsServiceResult_T()
    // {
    //     Assert.Equal(BenzeneResultStatus.Accepted, ServiceResult.Accepted().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.Ok, ServiceResult.Ok().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.Created, ServiceResult.Created().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.Updated, ServiceResult.Updated().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.Deleted, ServiceResult.Deleted().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable, ServiceResult.ValidationError<NullPayload>().AsType<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.NotFound, ServiceResult.NotFound().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable, ServiceResult.Forbidden().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable, ServiceResult.ServiceUnavailable().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.Conflict, ServiceResult.Conflict().AsServiceResult<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable, ServiceResult.Unauthorized().AsServiceResult<NullPayload>().Status);
    // }
    //
    // [Fact]
    // public void AsServiceResult_Payload()
    // {
    //     Assert.Equal(BenzeneResultStatus.Accepted, ServiceResult.Accepted<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.Ok, ServiceResult.Ok<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.Created, ServiceResult.Created<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.Updated, ServiceResult.Updated<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.Deleted, ServiceResult.Deleted<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable,
    //         ServiceResult.ValidationError<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.NotFound, ServiceResult.NotFound<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable, ServiceResult.Forbidden<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable,
    //         ServiceResult.ServiceUnavailable<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.Conflict, ServiceResult.Conflict<NullPayload>().AsServiceResult(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable, ServiceResult.Unauthorized<NullPayload>().AsServiceResult(new NullPayload()).Status);
    // }
    //
    // [Fact]
    // public void AsServiceResult_Task()
    // {
    //     Assert.Equal(BenzeneResultStatus.Ok, Task.FromResult(ServiceResult.Ok().AsServiceResult()).BenzeneResult.Status);
    // }
    //
    // [Fact]
    // public void AsType_T()
    // {
    //     Assert.Equal(BenzeneResultStatus.Accepted, ServiceResult.Accepted().AsType<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.Ok, ServiceResult.Ok().AsType<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.Created, ServiceResult.Created().AsType<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.Updated, ServiceResult.Updated().AsType<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.Deleted, ServiceResult.Deleted().AsType<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable, ServiceResult.ValidationError().AsType<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.NotFound, ServiceResult.NotFound().AsType<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable, ServiceResult.Forbidden().AsType<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable, ServiceResult.ServiceUnavailable().AsType<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.Conflict, ServiceResult.Conflict().AsType<NullPayload>().Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable, ServiceResult.Unauthorized().AsType<NullPayload>().Status);
    // }
    //
    // [Fact]
    // public void AsType()
    // {
    //     Assert.Equal(BenzeneResultStatus.Accepted, ServiceResult.Accepted<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.Ok, ServiceResult.Ok<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.Created, ServiceResult.Created<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.Updated, ServiceResult.Updated<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.Deleted, ServiceResult.Deleted<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable, ServiceResult.ValidationError<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.NotFound, ServiceResult.NotFound<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable, ServiceResult.Forbidden<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable, ServiceResult.ServiceUnavailable<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.Conflict, ServiceResult.Conflict<NullPayload>().AsType(new NullPayload()).Status);
    //     Assert.Equal(BenzeneResultStatus.ServiceUnavailable, ServiceResult.Unauthorized<NullPayload>().AsType(new NullPayload()).Status);
    // }
    //
    // [Fact]
    // public void MapIfSuccessful()
    // {
    //     Assert.Equal(BenzeneResultStatus.Ok, Task.FromResult(ServiceResult.Ok<NullPayload>()).MapIfSuccessful(x => x).BenzeneResult.Status);
    // }
    //
    // [Fact]
    // public void AsServiceResultMapIfSuccessful()
    // {
    //     Assert.Equal(BenzeneResultStatus.Ok, Task.FromResult(ServiceResult.Ok<NullPayload>()).AsServiceResultMapIfSuccessful(x => x).BenzeneResult.Status);
    // }
}

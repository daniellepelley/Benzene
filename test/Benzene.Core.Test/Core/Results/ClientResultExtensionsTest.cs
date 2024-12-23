// using System.Threading.Tasks;
// using Benzene.Results;
// using Benzene.Test.Clients.Samples;
// using Benzene.Test.Examples;
// using Xunit;
//
// namespace Benzene.Test.Core.Results;
//
// public class BenzeneResultExtensionsTest
// {
//     [Fact]
//     public void IsAccepted_True()
//     {
//         Assert.True(BenzeneResult.Accepted().IsAccepted());
//     }
//     [Fact]
//     public void IsAccepted_False()
//     {
//         Assert.False(BenzeneResult.UnexpectedError().IsAccepted());
//     }
//     [Fact]
//     public void IsCreated_True()
//     {
//         Assert.True(BenzeneResult.Created().IsCreated());
//     }
//     [Fact]
//     public void IsCreated_False()
//     {
//         Assert.False(BenzeneResult.UnexpectedError().IsCreated());
//     }
//     [Fact]
//     public void IsUpdated_True()
//     {
//         Assert.True(BenzeneResult.Updated().IsUpdated());
//     }
//     [Fact]
//     public void IsUpdated_False()
//     {
//         Assert.False(BenzeneResult.UnexpectedError().IsUpdated());
//     }
//     [Fact]
//     public void IsDeleted_True()
//     {
//         Assert.True(BenzeneResult.Deleted().IsDeleted());
//     }
//     [Fact]
//     public void IsDeleted_False()
//     {
//         Assert.False(BenzeneResult.UnexpectedError().IsDeleted());
//     }
//     [Fact]
//     public void IsIgnored_True()
//     {
//         Assert.True(BenzeneResult.Ignored().IsIgnored());
//     }
//     [Fact]
//     public void IsIgnored_False()
//     {
//         Assert.False(BenzeneResult.UnexpectedError().IsIgnored());
//     }
//
//     [Fact]
//     public void IsNotFound_True()
//     {
//         Assert.True(BenzeneResult.NotFound().IsNotFound());
//     }
//     [Fact]
//     public void IsNotFound_False()
//     {
//         Assert.False(BenzeneResult.UnexpectedError().IsNotFound());
//     }
//     [Fact]
//     public void IsBadRequest_True()
//     {
//         Assert.True(BenzeneResult.BadRequest().IsBadRequest());
//     }
//     [Fact]
//     public void IsBadRequest_False()
//     {
//         Assert.False(BenzeneResult.UnexpectedError().IsBadRequest());
//     }
//     [Fact]
//     public void IsValidationError_True()
//     {
//         Assert.True(BenzeneResult.ValidationError().IsValidationError());
//     }
//     [Fact]
//     public void IsValidationError_False()
//     {
//         Assert.False(BenzeneResult.UnexpectedError().IsValidationError());
//     }
//     [Fact]
//     public void IsServiceUnavailable_True()
//     {
//         Assert.True(BenzeneResult.ServiceUnavailable().IsServiceUnavailable());
//     }
//     [Fact]
//     public void IsServiceUnavailable_False()
//     {
//         Assert.False(BenzeneResult.UnexpectedError().IsServiceUnavailable());
//     }
//     [Fact]
//     public void IsNotImplemented_True()
//     {
//         Assert.True(BenzeneResult.NotImplemented().IsNotImplemented());
//     }
//     [Fact]
//     public void IsNotImplemented_False()
//     {
//         Assert.False(BenzeneResult.UnexpectedError().IsNotImplemented());
//     }
//     [Fact]
//     public void IsUnexpectedError_True()
//     {
//         Assert.True(BenzeneResult.UnexpectedError().IsUnexpectedError());
//     }
//     [Fact]
//     public void IsUnexpectedError_False()
//     {
//         Assert.False(BenzeneResult.Ok().IsUnexpectedError());
//     }
//     [Fact]
//     public void IsConflict_True()
//     {
//         Assert.True(BenzeneResult.Conflict().IsConflict());
//     }
//     [Fact]
//     public void IsConflict_False()
//     {
//         Assert.False(BenzeneResult.UnexpectedError().IsConflict());
//     }
//     [Fact]
//     public void IsForbidden_True()
//     {
//         Assert.True(BenzeneResult.Forbidden().IsForbidden());
//     }
//     [Fact]
//     public void IsForbidden_False()
//     {
//         Assert.False(BenzeneResult.UnexpectedError().IsForbidden());
//     }
//     [Fact]
//     public void IsUnauthorized_True()
//     {
//         Assert.True(BenzeneResult.Unauthorized().IsUnauthorized());
//     }
//     [Fact]
//     public void IsUnauthorized_False()
//     {
//         Assert.False(BenzeneResult.UnexpectedError().IsUnauthorized());
//     }
//
//     [Fact]
//     public void AsServiceResult_T()
//     {
//         var result = BenzeneResult.Ok(new ExamplePayload()).AsServiceResult();
//         Assert.Equal(BenzeneResultStatus.Ok, result.Status);
//     }
//
//     [Fact]
//     public void AsServiceResult()
//     {
//         var result = BenzeneResult.Accepted().AsServiceResult();
//         Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
//     }
//
//     [Fact]
//     public void AsServiceResultTask_T()
//     {
//         var result = Task.FromResult(BenzeneResult.Accepted(new ExamplePayload())).AsServiceResult().Result;
//         Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
//     }
//
//     [Fact]
//     public void AsServiceResultTask()
//     {
//         var result = Task.FromResult(BenzeneResult.Accepted()).AsServiceResult().Result;
//         Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
//     }
//
//     [Fact]
//     public void AsServiceResult_T2()
//     {
//         var result = BenzeneResult.Accepted().AsServiceResult<ExamplePayload>();
//         Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
//     }
//
//     [Fact]
//     public void As()
//     {
//         var result = BenzeneResult.Created().As<ExampleResponsePayload>();
//         Assert.Equal(BenzeneResultStatus.Created, result.Status);
//     }
//
//     [Fact]
//     public void As2()
//     {
//         var exampleResponsePayload = new ExampleResponsePayload { Name = "foo" };
//         var result = BenzeneResult.Created(new ExamplePayload()).As(exampleResponsePayload);
//         Assert.Equal(BenzeneResultStatus.Created, result.Status);
//         Assert.Equal("foo", result.Payload.Name);
//     }
//
//     [Fact]
//     public void MapIfSuccessful()
//     {
//         var result = Task.FromResult(BenzeneResult.Ok(new ExamplePayload { Name = "foo" }))
//             .MapIfSuccessful(x => new ExampleResponsePayload { Name = x.Name })
//             .Result;
//         Assert.Equal(BenzeneResultStatus.Ok, result.Status);
//         Assert.Equal("foo", result.Payload.Name);
//     }
//
//     [Fact]
//     public void MapIfSuccessful_Errors()
//     {
//         var result = Task.FromResult(BenzeneResult.BadRequest<ExamplePayload>("foo"))
//             .MapIfSuccessful(x => new ExampleResponsePayload { Name = x.Name })
//             .Result;
//         Assert.Equal(BenzeneResultStatus.BadRequest, result.Status);
//         Assert.Equal("foo", result.Errors[0]);
//     }
//
//     [Fact]
//     public void AsServiceResultMapIfSuccessful()
//     {
//         var result = Task.FromResult(BenzeneResult.Ok(new ExamplePayload { Name = "foo" }))
//             .AsServiceResultMapIfSuccessful(x => new ExampleResponsePayload { Name = x.Name })
//             .Result;
//         Assert.Equal(BenzeneResultStatus.Ok, result.Status);
//         Assert.Equal("foo", result.Payload.Name);
//     }
// }

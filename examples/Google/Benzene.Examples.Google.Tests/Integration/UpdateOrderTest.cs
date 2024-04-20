// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using Amazon.Lambda.TestUtilities;
// using Benzene.Core.Results;
// using Benzene.Examples.Aws.Handlers;
// using Benzene.Examples.Aws.Model;
// using Benzene.Examples.Aws.Model.Messages;
// using Benzene.Examples.Google.Tests.Helpers;
// using Benzene.Examples.Google.Tests.Helpers.Builders;
// using Xunit;
//
// namespace Benzene.Examples.Google.Tests.Integration
// {
//     [Trait("Category", "Integration")]
//     [Collection("Sequential")]
//     public class UpdateOrderTest : IntegrationTestBase
//     {
//         private const string CreateOrder = MessageTopicNames.OrderCreate;
//         private const string UpdateOrder = MessageTopicNames.OrderUpdate;
//         private const string SomeStatus = "some-status";
//         private const string SomeName = "some-name";
//         private const string UpdatedStatus = "updated-crn";
//         private const string UpdatedName = "updated-name";
//         private CreateOrderMessage CreateOrderMessage()
//         {
//             return new CreateOrderMessage
//             {
//                 Name = Defaults.Order.Name,
//                 Status = Defaults.Order.Status,
//             };
//         }
//
//         [Fact]
//         public async Task UpdateOrder_SNSEvent()
//         {
//             //create order event
//             var snsEvent = AwsEventBuilder.CreateSnsEvent(CreateOrder, CreateOrderMessage());
//             await TestLambdaHosting.SendEventAsync(snsEvent);
//             var orders = DatabaseSetup.CreateDataContext().Orders.First();
//
//             //Update order
//             var updatedJsonStr = new { orders.Id, Status = UpdatedStatus, Name = UpdatedName };
//             var snsUpdateEvent = AwsEventBuilder.CreateSnsEvent(UpdateOrder, updatedJsonStr);
//             await TestLambdaHosting.SendEventAsync(snsUpdateEvent);
//             var order = DatabaseSetup.CreateDataContext().Orders.ToArray();
//
//             Assert.Single(order);
//             Assert.Equal(UpdatedStatus, order[0].Status);
//             Assert.Equal(UpdatedName, order[0].Name);
//
//             var messages = await SqsSetUp.GetAllMessagesAsync();
//             Assert.Equal("204", messages[2].GetStatus());
//             Assert.Equal($"{UpdateOrder}:result", messages[2].GetTopic());
//
//             Assert.Equal($"{UpdateOrder}d", messages[3].GetTopic());
//             Assert.Null(messages[3].GetStatus());
//             var payload = messages[3].Body<OrderDto>();
//             Assert.Equal(UpdatedStatus, payload.Status);
//             Assert.Equal(UpdatedName, payload.Name);
//         }
//
//         [Fact]
//         public async Task UpdateOrder_SQSEvent()
//         {
//             //create person event
//             var sqsCreateEvent = AwsEventBuilder.CreateSqsEvent(CreateOrder, CreateOrderMessage());
//             await TestLambdaHosting.SendEventAsync(sqsCreateEvent);
//             var orders = DatabaseSetup.CreateDataContext().Orders.First();
//
//             //Update order
//             var updatedJsonStr = new { orders.Id, Status = UpdatedStatus, Name = UpdatedName };
//             var snsUpdateEvent = AwsEventBuilder.CreateSqsEvent(UpdateOrder, updatedJsonStr);
//             await TestLambdaHosting.SendEventAsync(snsUpdateEvent);
//             var order = DatabaseSetup.CreateDataContext().Orders.ToArray();
//
//             Assert.Single(order);
//             Assert.Equal(UpdatedStatus, order[0].Status);
//             Assert.Equal(UpdatedName, order[0].Name);
//
//             var messages = await SqsSetUp.GetAllMessagesAsync();
//             Assert.Equal("204", messages[2].GetStatus());
//             Assert.Equal($"{UpdateOrder}:result", messages[2].GetTopic());
//
//             Assert.Equal($"{UpdateOrder}d", messages[3].GetTopic());
//             Assert.Null(messages[3].GetStatus());
//             var payload = messages[3].Body<OrderDto>();
//             Assert.Equal(UpdatedStatus, payload.Status);
//             Assert.Equal(UpdatedName, payload.Name);
//         }
//
//         [Fact]
//         public async Task UpdateOrder_ValidationFailure()
//         {
//             var snsUpdateEvent = AwsEventBuilder.CreateSnsEvent(UpdateOrder, new UpdateOrderMessage { Status = "" });
//
//             await TestLambdaHosting.SendEventAsync(snsUpdateEvent);
//
//             var messages = await SqsSetUp.GetAllMessagesAsync();
//             Assert.Equal($"{UpdateOrder}:result", messages[0].GetTopic());
//             var errorPayload = messages[0].Body<ErrorPayload>();
//             Assert.Equal(Defaults.ErrorStatus.ValidationError, errorPayload.Status);
//             Assert.NotEmpty(errorPayload.Errors);
//
//             Assert.Single(messages);
//         }
//
//         [Fact]
//         public async Task UpdateOrder_InvalidFKi_NotFound()
//         {
//             //Update order
//             var updatedJsonStr = new { Defaults.Order.Id, Status = UpdatedStatus, Name = UpdatedName };
//             var snsUpdateEvent = AwsEventBuilder.CreateSnsEvent(UpdateOrder, updatedJsonStr);
//             await TestLambdaHosting.SendEventAsync(snsUpdateEvent);
//
//             var messages = await SqsSetUp.GetAllMessagesAsync();
//             Assert.Equal($"{UpdateOrder}:result", messages[0].GetTopic());
//             var errorPayload = messages[0].Body<ErrorPayload>();
//             Assert.Equal(Defaults.ErrorStatus.NotFound, errorPayload.Status);
//         }
//
//         [Fact]
//         public async Task UpdateOrder_ThreadSafety()
//         {
//             await DatabaseSetup.ResetDatabaseAsync();
//
//             var snsEvent = AwsEventBuilder.CreateSnsEvent(CreateOrder, CreateOrderMessage());
//             var sqsEvent = AwsEventBuilder.CreateSqsEvent(CreateOrder, CreateOrderMessage());
//
//             var lambdaContext = new TestLambdaContext();
//             var testLogger = new ThreadSafeTestLambdaLogger();
//             lambdaContext.Logger = testLogger;
//             var threads1 = Enumerable.Range(0, 10)
//                 .Select(_ => new Thread(() => TestLambdaHosting.SendEventAsync(snsEvent, lambdaContext).Wait())).ToArray();
//             var threads2 = Enumerable.Range(0, 10)
//                 .Select(_ => new Thread(() => TestLambdaHosting.SendEventAsync(sqsEvent, lambdaContext).Wait())).ToArray();
//
//             foreach (var thread in threads1.Concat(threads2))
//             {
//                 thread.Start();
//             }
//
//             foreach (var thread in threads1.Concat(threads2))
//             {
//                 thread.Join(1000);
//             }
//
//             var orders = DatabaseSetup.CreateDataContext().Orders.ToArray();
//
//             Assert.Equal(20, orders.Length);
//
//             for (var i = 0; i < 20; i++)
//             {
//                 Assert.Equal(Defaults.Order.Status, orders[i].Status);
//                 Assert.Equal(Defaults.Order.Name, orders[i].Name);
//             }
//         }
//     }
// }

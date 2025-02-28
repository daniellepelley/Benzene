// using System.Collections.Generic;
// using Amazon.SimpleNotificationService.Model;
// using Benzene.Abstractions.Messages.BenzeneClient;
// using Benzene.Abstractions.Serialization;
// using Benzene.Clients.Common;
//
// namespace Benzene.Clients.Aws.Sns;
//
// public class SnsClientRequestMapper : IClientRequestMapper<PublishRequest>
// {
//     private readonly ISerializer _serializer;
//     private string _topicArn;
//
//     public SnsClientRequestMapper(string topicArn, ISerializer serializer)
//     {
//         _topicArn = topicArn;
//         _serializer = serializer;
//     }
//
//     public PublishRequest CreateRequest<TRequest>(IBenzeneClientRequest<TRequest> request)
//     {
//         return new PublishRequest
//         {
//             TopicArn = _topicArn, 
//             Message= _serializer.Serialize(request.Message),
//             MessageAttributes = new Dictionary<string, MessageAttributeValue>
//             {
//                 { "topic", new MessageAttributeValue { StringValue = request.Topic, DataType = "String"} }
//             }
//         };
//     }
// }
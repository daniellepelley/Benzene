// using Benzene.Abstractions.Request;
// using Benzene.Core.Request;
// using Newtonsoft.Json;
//
// namespace Benzene.Kafka.Core.KafkaMessage;
//
// public class KafkaRequestMapper<Tkey, TValue> : IRequestMapper<KafkaRecordContext<Tkey, TValue>>
// {
//     public TRequest GetBody<TRequest>(KafkaRecordContext<Tkey, TValue> context) where TRequest : class
//     {
//         if (typeof(TRequest) == typeof(string))
//         {
//             return GetBody(context) as TRequest;
//         }
//
//         return JsonConvert.DeserializeObject<TRequest>(GetBody(context));
//     }
//
//     public string GetBody(KafkaRecordContext<Tkey, TValue> context)
//     {
//         return context.ConsumeResult.Message.Value.ToString();
//     }
// }
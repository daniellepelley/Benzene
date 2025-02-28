// using Benzene.Clients;
// using Benzene.Core.Logging;
// using Benzene.Core.Middleware;
// using Benzene.Kafka.Core.Kafka;
// using Confluent.Kafka;
// using Newtonsoft.Json;
// using Void = Benzene.Abstractions.Results.Void;
//
// namespace Benzene.Examples.Kakfa.Producer;
//
// public class KafkaSender : IDisposable
// {
//     private readonly IProducer<string, string>? _producer;
//     private KafkaBenzeneMessageClient _kafkaClient;
//
//     public KafkaSender(ProducerConfig config)
//     {
//         _producer = new ProducerBuilder<string, string>(config).Build();
//
//         var benzeneServiceContainer = new NullBenzeneServiceContainer();
//         var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<KafkaSendMessageContext>(benzeneServiceContainer);
//         var middlewarePipeline = middlewarePipelineBuilder
//             .UseKafkaClient(_producer)
//             .Build();
//
//         _kafkaClient = new KafkaBenzeneMessageClient(middlewarePipeline, BenzeneLogger.NullLogger);
//     }
//
//     public void Dispose()
//     {
//         _producer?.Flush();
//         _producer?.Dispose();
//     }
//
//     public async Task SendAsync(string topic, object message)
//     {
//         await _kafkaClient.SendMessageAsync<object, Void>(topic, message);
//     }
// }
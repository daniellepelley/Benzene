namespace Benzene.Abstractions.MessageHandlers.Info
{
    /// <summary>
    /// The canonical name for each transport Benzene ships an adapter for — the single source of
    /// truth both <c>TransportMiddlewarePipeline&lt;TContext&gt;</c>'s runtime, per-invocation tag
    /// (<see cref="ISetCurrentTransport"/>/<see cref="ICurrentTransport"/>) and each transport
    /// package's startup-time <see cref="ITransportInfo"/> DI registration (aggregated by
    /// <see cref="ITransportsInfo"/>) are meant to use, so the two mechanisms can't silently drift
    /// apart into two different names for the same transport.
    /// </summary>
    public static class TransportNames
    {
        /// <summary>
        /// The value <see cref="ICurrentTransport.Name"/> reports before any transport pipeline has
        /// recorded itself — i.e. "no transport resolved yet". Observability decorators skip the
        /// transport tag while it still reads this, so a probe/pass-through stage (e.g. an SQS handler
        /// declining an SNS event in a multi-transport function) isn't annotated with a sentinel. Kept
        /// at the historic <c>&lt;missing&gt;</c> value so metric buckets and existing dashboards don't shift.
        /// </summary>
        public const string Unresolved = "<missing>";

        public const string Http = "http";
        public const string Asp = "asp";
        public const string ApiGateway = "api-gateway";
        public const string Grpc = "grpc";
        public const string Benzene = "benzene";
        public const string Sqs = "sqs";
        public const string Sns = "sns";
        public const string S3 = "s3";
        public const string DynamoDb = "dynamodb";
        public const string Kinesis = "kinesis";
        public const string EventBridge = "eventbridge";
        public const string Kafka = "kafka";
        public const string RabbitMq = "rabbitmq";
        public const string PubSub = "pubsub";
        public const string QueueStorage = "queue-storage";
        public const string ServiceBus = "service-bus";
        public const string EventHub = "event-hub";
        public const string EventGrid = "event-grid";
        public const string BlobStorage = "blob-storage";
        public const string CosmosDb = "cosmos-db";
        public const string Timer = "timer";
    }
}

namespace Benzene.Abstractions.MessageHandlers.Info
{
    /// <summary>Describes a single transport the application can receive messages over (e.g. "Http", "Kafka", "Sqs").</summary>
    public interface ITransportInfo
    {
        /// <summary>The transport's name.</summary>
        string Name { get; }
    }
}

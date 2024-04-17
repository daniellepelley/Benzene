namespace Benzene.Aws.Sqs.Consumer;

public class SqsConsumerConfig
{
    public string ServiceUrl { get; set; }
    public string QueueUrl { get; set; }
    public int MaxNumberOfMessages { get; set; }
}

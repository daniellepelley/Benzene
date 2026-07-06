namespace Benzene.Aws.Sqs.Consumer;

public class SqsConsumerConfig
{
    public string QueueUrl { get; set; }
    public int MaxNumberOfMessages { get; set; }
}

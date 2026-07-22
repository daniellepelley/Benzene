using System.Linq;
using Benzene.Azure.Function.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Benzene.Test.Autogen.AzureFunctions;

// Drives AzureFunctionTriggerGenerator directly (CSharpGeneratorDriver, not the flaky
// Microsoft.CodeAnalysis.Testing harness the message-handler generator test skips). Stub attributes
// stand in for the real Benzene.Azure.Function.* ones so the test needs no Azure SDK packages; the
// generator matches them by metadata name via ForAttributeWithMetadataName. Asserts the emitted
// [Function], binding attribute, and dispatch per transport - the shapes proven end-to-end for
// HTTP/Queue/Service Bus via functions.metadata, locked for all nine here.
public class AzureFunctionTriggerGeneratorTest
{
    // No `using` (assembly attributes must precede a using), so System.* types are fully qualified.
    private const string StubAttributes = @"
namespace Benzene.Azure.Function.AspNet { [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)] public sealed class BenzeneHttpTriggerAttribute : System.Attribute { public string Name {get;set;} public string Route {get;set;} } }
namespace Benzene.Azure.Function.ServiceBus { [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)] public sealed class BenzeneServiceBusTriggerAttribute : System.Attribute { public string Name {get;set;} public string QueueName {get;set;} public string TopicName {get;set;} public string SubscriptionName {get;set;} public string Connection {get;set;} } }
namespace Benzene.Azure.Function.EventHub { [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)] public sealed class BenzeneEventHubTriggerAttribute : System.Attribute { public string Name {get;set;} public string EventHubName {get;set;} public string Connection {get;set;} public string ConsumerGroup {get;set;} } }
namespace Benzene.Azure.Function.Kafka { [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)] public sealed class BenzeneKafkaTriggerAttribute : System.Attribute { public string Name {get;set;} public string BrokerList {get;set;} public string Topic {get;set;} public string ConsumerGroup {get;set;} } }
namespace Benzene.Azure.Function.QueueStorage { [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)] public sealed class BenzeneQueueTriggerAttribute : System.Attribute { public string Name {get;set;} public string QueueName {get;set;} public string Connection {get;set;} } }
namespace Benzene.Azure.Function.BlobStorage { [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)] public sealed class BenzeneBlobTriggerAttribute : System.Attribute { public string Name {get;set;} public string Path {get;set;} public string Connection {get;set;} } }
namespace Benzene.Azure.Function.EventGrid { [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)] public sealed class BenzeneEventGridTriggerAttribute : System.Attribute { public string Name {get;set;} } }
namespace Benzene.Azure.Function.CosmosDb { [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)] public sealed class BenzeneCosmosDbTriggerAttribute : System.Attribute { public string Name {get;set;} public System.Type DocumentType {get;set;} public string DatabaseName {get;set;} public string ContainerName {get;set;} public string Connection {get;set;} public string LeaseContainerName {get;set;} public bool CreateLeaseContainerIfNotExists {get;set;} } }
namespace Benzene.Azure.Function.Timer { [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)] public sealed class BenzeneTimerTriggerAttribute : System.Attribute { public string Name {get;set;} public string Schedule {get;set;} public bool RunOnStartup {get;set;} } }
namespace App { public class OrderDoc { } }
";

    private static string Generate(string declarations)
    {
        var compilation = CSharpCompilation.Create(
            "TestAsm",
            // Assembly attributes must lexically precede namespace/type declarations, so the
            // declarations come first, then the stub attribute definitions they reference.
            new[] { CSharpSyntaxTree.ParseText(declarations + "\n" + StubAttributes) },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver
            .Create(new AzureFunctionTriggerGenerator().AsSourceGenerator())
            .RunGenerators(compilation);

        return string.Join("\n\n", driver.GetRunResult().GeneratedTrees.Select(t => t.ToString()));
    }

    [Fact]
    public void Http_EmitsFunctionRouteAndDispatch()
    {
        var output = Generate(@"[assembly: Benzene.Azure.Function.AspNet.BenzeneHttpTrigger(Name = ""orders"", Route = ""{*restOfPath}"")]");

        Assert.Contains(@"[global::Microsoft.Azure.Functions.Worker.Function(""orders"")]", output);
        Assert.Contains("global::Microsoft.Azure.Functions.Worker.HttpTrigger(", output);
        Assert.Contains(@"Route = ""{*restOfPath}""", output);
        Assert.Contains("global::Microsoft.AspNetCore.Http.HttpRequest req", output);
        Assert.Contains("global::Benzene.Azure.Function.AspNet.Extensions.HandleHttpRequest(_app, req)", output);
    }

    [Fact]
    public void ServiceBus_Queue_EmitsQueueBindingAndDispatch()
    {
        var output = Generate(@"[assembly: Benzene.Azure.Function.ServiceBus.BenzeneServiceBusTrigger(Name = ""sb"", QueueName = ""orders"", Connection = ""ServiceBusConnection"")]");

        Assert.Contains(@"global::Microsoft.Azure.Functions.Worker.ServiceBusTrigger(""orders"", Connection = ""ServiceBusConnection"")", output);
        Assert.Contains("global::Azure.Messaging.ServiceBus.ServiceBusReceivedMessage message", output);
        Assert.Contains("HandleServiceBusMessages(_app, message)", output);
    }

    [Fact]
    public void ServiceBus_Topic_EmitsTopicAndSubscription()
    {
        var output = Generate(@"[assembly: Benzene.Azure.Function.ServiceBus.BenzeneServiceBusTrigger(Name = ""sb"", TopicName = ""audit"", SubscriptionName = ""svc"")]");

        Assert.Contains(@"global::Microsoft.Azure.Functions.Worker.ServiceBusTrigger(""audit"", ""svc"", Connection = ""ServiceBusConnection"")", output);
    }

    [Fact]
    public void EventHub_EmitsBindingWithConsumerGroup()
    {
        var output = Generate(@"[assembly: Benzene.Azure.Function.EventHub.BenzeneEventHubTrigger(Name = ""eh"", EventHubName = ""telemetry"", ConsumerGroup = ""$Default"")]");

        Assert.Contains(@"global::Microsoft.Azure.Functions.Worker.EventHubTrigger(""telemetry"", Connection = ""EventHubConnection"", ConsumerGroup = ""$Default"")", output);
        Assert.Contains("global::Azure.Messaging.EventHubs.EventData[] events", output);
        Assert.Contains("HandleEventHub(_app, events)", output);
    }

    [Fact]
    public void Kafka_EmitsBrokerTopicAndRecordArray()
    {
        var output = Generate(@"[assembly: Benzene.Azure.Function.Kafka.BenzeneKafkaTrigger(Name = ""k"", BrokerList = ""BrokerList"", Topic = ""orders"", ConsumerGroup = ""svc"")]");

        Assert.Contains(@"global::Microsoft.Azure.Functions.Worker.KafkaTrigger(""BrokerList"", ""orders"", ConsumerGroup = ""svc"")", output);
        Assert.Contains("global::Benzene.Azure.Function.Kafka.KafkaRecord[] events", output);
        Assert.Contains("HandleKafkaEvents(_app, events)", output);
    }

    [Fact]
    public void Queue_EmitsBindingAndStringParam()
    {
        var output = Generate(@"[assembly: Benzene.Azure.Function.QueueStorage.BenzeneQueueTrigger(Name = ""q"", QueueName = ""orders"")]");

        Assert.Contains(@"global::Microsoft.Azure.Functions.Worker.QueueTrigger(""orders"", Connection = ""AzureWebJobsStorage"")", output);
        Assert.Contains("] string messageText", output);
        Assert.Contains("HandleQueueMessage(_app, messageText)", output);
    }

    [Fact]
    public void Blob_EmitsTwoParametersAndNameFirstDispatch()
    {
        var output = Generate(@"[assembly: Benzene.Azure.Function.BlobStorage.BenzeneBlobTrigger(Name = ""b"", Path = ""incoming/{name}"")]");

        Assert.Contains(@"global::Microsoft.Azure.Functions.Worker.BlobTrigger(""incoming/{name}"", Connection = ""AzureWebJobsStorage"")", output);
        Assert.Contains("] byte[] content, string name", output);
        Assert.Contains("HandleBlob(_app, name, content)", output);
    }

    [Fact]
    public void EventGrid_EmitsStringBinding()
    {
        var output = Generate(@"[assembly: Benzene.Azure.Function.EventGrid.BenzeneEventGridTrigger(Name = ""eg"")]");

        Assert.Contains("[global::Microsoft.Azure.Functions.Worker.EventGridTrigger] string eventJson", output);
        Assert.Contains("HandleEventGridEvent(_app, eventJson)", output);
    }

    [Fact]
    public void CosmosDb_EmitsGenericOverDocumentType()
    {
        var output = Generate(@"[assembly: Benzene.Azure.Function.CosmosDb.BenzeneCosmosDbTrigger(Name = ""c"", DocumentType = typeof(App.OrderDoc), DatabaseName = ""shop"", ContainerName = ""orders"", CreateLeaseContainerIfNotExists = true)]");

        Assert.Contains("databaseName: \"shop\"", output);
        Assert.Contains("containerName: \"orders\"", output);
        Assert.Contains("CreateLeaseContainerIfNotExists = true", output);
        Assert.Contains("global::System.Collections.Generic.IReadOnlyList<global::App.OrderDoc> documents", output);
        Assert.Contains("HandleCosmosDbChanges<global::App.OrderDoc>(_app, documents)", output);
    }

    [Fact]
    public void CosmosDb_WithoutDocumentType_EmitsNothing()
    {
        var output = Generate(@"[assembly: Benzene.Azure.Function.CosmosDb.BenzeneCosmosDbTrigger(Name = ""c"", DatabaseName = ""shop"", ContainerName = ""orders"")]");

        Assert.DoesNotContain("CosmosDBTrigger", output);
    }

    [Fact]
    public void Timer_EmitsScheduleRunOnStartupAndNoArgDispatch()
    {
        var output = Generate(@"[assembly: Benzene.Azure.Function.Timer.BenzeneTimerTrigger(Name = ""t"", Schedule = ""0 */1 * * * *"", RunOnStartup = true)]");

        Assert.Contains(@"global::Microsoft.Azure.Functions.Worker.TimerTrigger(""0 */1 * * * *"", RunOnStartup = true)", output);
        Assert.Contains("global::Microsoft.Azure.Functions.Worker.TimerInfo timer", output);
        Assert.Contains("HandleTimer(_app)", output);
    }

    [Fact]
    public void MultipleDeclarations_EmitOneClassEach()
    {
        var output = Generate(
            @"[assembly: Benzene.Azure.Function.QueueStorage.BenzeneQueueTrigger(Name = ""a"", QueueName = ""qa"")]" +
            @"[assembly: Benzene.Azure.Function.QueueStorage.BenzeneQueueTrigger(Name = ""b"", QueueName = ""qb"")]");

        Assert.Contains(@"[global::Microsoft.Azure.Functions.Worker.Function(""a"")]", output);
        Assert.Contains(@"[global::Microsoft.Azure.Functions.Worker.Function(""b"")]", output);
    }
}

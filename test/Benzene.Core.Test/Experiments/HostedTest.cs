using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Aws.Lambda.Sqs.TestHelpers;
using Benzene.Aws.Sqs;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Clients.Aws.Sns;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.MessageSender;
using Benzene.Core.Middleware;
using Benzene.HostedService;
using Benzene.Kafka.Core;
using Benzene.Microsoft.Dependencies;
using Benzene.SelfHost;
using Benzene.Test.Aws.Sns;
using Benzene.Test.Examples;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Benzene.Test.Experiments;

public class HostedTest
{
    [Fact]
    public void Test()
    {
        // var list = new List<string>();
        //
        // var inlineSelfHostedStartUp = new InlineSelfHostedStartUp();
        // inlineSelfHostedStartUp.Configure(x => x.UseSqs(new SqsConsumerConfig(), x => x
        //     .OnRequest(r => list.Add(r.Message.Body))
        // ));
        //
        //
        // var host = new HostBuilder()
        //     .ConfigureServices(services =>
        //     {
        //         services.AddHostedService(x => inlineSelfHostedStartUp.BuildHostedService());
        //     })
        //     .Build();
        //
        // host.Start();
        
    }
}


using System;
using System.Threading.Tasks;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.Timer;
using Benzene.Core.MessageHandlers;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Azure;

public class TimerPipelineTest
{
    [Fact]
    public async Task Tick_IsDeliveredToTheTickStep_WithScheduleInfo()
    {
        TimerTriggerInfo observed = null;
        var next = DateTimeOffset.UtcNow.AddMinutes(5);

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseTimerTrigger(timer => timer
                    .UseTick(info =>
                    {
                        observed = info;
                        return Task.CompletedTask;
                    })))
            .Build();

        await app.HandleTimer(new TimerTriggerInfo
        {
            IsPastDue = true,
            ScheduleStatus = new TimerScheduleStatus { Next = next }
        });

        Assert.NotNull(observed);
        Assert.True(observed.IsPastDue);
        Assert.Equal(next, observed.ScheduleStatus.Next);
    }

    [Fact]
    public async Task Tick_WithPresetTopic_InvokesTheMessageHandler()
    {
        var mockExampleService = new Mock<IExampleService>();

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object)
            ).Configure(app => app
                .UseTimerTrigger(timer => timer
                    .UsePresetTopic(Defaults.Topic)
                    .UseMessageHandlers()))
            .Build();

        await app.HandleTimer();

        // The tick's body carries only schedule info, so the handler's payload binds with a null
        // Name - what's proven here is that the tick routed to and invoked the preset topic's handler.
        mockExampleService.Verify(x => x.Register(null), Times.Once);
    }

    [Fact]
    public async Task TickException_Propagates_SoTheHostRecordsTheFailure()
    {
        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseTimerTrigger(timer => timer
                    .UseTick((TimerContext _) => throw new InvalidOperationException("job failed"))))
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => app.HandleTimer());
    }

    [Fact]
    public void PlatformNeutralOverload_NoOpsOnNonAzureBuilders()
    {
        var mockBuilder = new Mock<Benzene.Abstractions.Hosting.IBenzeneApplicationBuilder>();

        var result = mockBuilder.Object.UseTimerTrigger(timer => timer
            .UseTick((TimerContext _) => Task.CompletedTask));

        Assert.Same(mockBuilder.Object, result);
    }
}

using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Clients.Aws.Sns;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Experiments;

public class ConvertTest
{
    [Fact]
    public async Task Convert()
    {
        var services = new MicrosoftBenzeneServiceContainer(new ServiceCollection());
        var appBuilder = new MiddlewarePipelineBuilder<string>(services);
        var appBuilder2 = new MiddlewarePipelineBuilder<StringConvert>(services);

        var app = appBuilder
            .Convert(
                x => new StringConvert { Value = x },
            (_, _) => { },
                appBuilder2.Build())
            .Build();

        var entryPoint =
            new EntryPointMiddlewareApplication<string>(new MiddlewareApplication<string, string>(app, s => s),
                services.CreateServiceResolverFactory());

        await entryPoint.SendAsync("foo");
    }
}

public class StringConvert
{
    public string Value { get; set; }
}


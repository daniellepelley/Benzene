using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.App.Model;
using Benzene.FluentValidation;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Examples.Aws;

public class BareMetalLambdaEntryPoint
{
    private readonly IMiddlewarePipelineBuilder<AwsEventStreamContext> _app;
    private readonly MicrosoftServiceResolverFactory _microsoftServiceResolverFactory;
    private readonly IMiddlewarePipeline<AwsEventStreamContext> _pipeline;

    public BareMetalLambdaEntryPoint()
    {
        var services = new ServiceCollection();
        
        var middlewarePipelineBuilder = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(services))
            .UseBenzeneMessage(x =>
                x.UseMessageHandlers(s =>
                    s.UseFluentValidation()));

        services.UsingBenzene(x => x
            .AddMessageHandlers(Assembly.GetAssembly(typeof(OrderDto))));

        // services.AddScoped<IOrderDbClient, OrderDbClient>();
        // services.AddScoped<IOrderService, OrderService>();

        // services.AddDbContext<DataContext>(x => x.UseNpgsql(configuration["DB_CONNECTION_STRING"],
        //     pgOptions => pgOptions.ProvidePasswordCallback(DbConnectionStringFactory.PasswordCallback())));

        // services.AddScoped<IDataContext>(x => new OrderDataContext(x.GetService<DataContext>()));

        _microsoftServiceResolverFactory = new MicrosoftServiceResolverFactory(services.BuildServiceProvider());
        _pipeline = middlewarePipelineBuilder.Build();
    }

    public async Task<Stream> FunctionHandler(Stream input, ILambdaContext lambdaContext)
    {
        using var serviceResolver = _microsoftServiceResolverFactory.CreateScope();
        
        var context = new AwsEventStreamContext(input, lambdaContext);
        await _pipeline.HandleAsync(context, serviceResolver);
        return context.Response;
    }
}
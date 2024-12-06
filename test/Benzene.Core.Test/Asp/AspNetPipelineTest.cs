// using System;
// using System.IO;
// using System.IO.Pipelines;
// using System.Security.Claims;
// using System.Security.Principal;
// using System.Threading;
// using System.Threading.Tasks;
// using Benzene.Abstractions.Response;
// using Benzene.Core.DI;
// using Benzene.Core.MessageHandling;
// using Benzene.Core.MiddlewareBuilder;
// using Benzene.Core.Response;
// using Benzene.Http;
// using Benzene.AspNet.Core;
// using Benzene.Microsoft.Dependencies;
// using Benzene.Test.Clients.Samples;
// using Benzene.Test.Examples;
// using Microsoft.AspNetCore.Http;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Logging.Abstractions;
// using Moq;
// using Xunit;

// namespace Benzene.AspNet.Core.Test;

// public class StubHttpResponse : HttpResponse
// {
//     public override HttpContext HttpContext { get; }
//     public override int StatusCode { get; set; }
//     public override IHeaderDictionary Headers { get; } = new HeaderDictionary();
//     public override Stream Body { get; set; } = new MemoryStream();
//     public override long? ContentLength { get; set; }
//     public override string ContentType { get; set; }
//     public override IResponseCookies Cookies { get; }
//     public override bool HasStarted { get; }
//     public override PipeWriter BodyWriter => Mock.Of<PipeWriter>();

//     public override void OnStarting(Func<object, Task> callback, object state)
//     {
//     }

//     public override void OnCompleted(Func<object, Task> callback, object state)
//     {
//     }

//     public override void Redirect(string location, bool permanent)
//     {
//     }

//     public override Task StartAsync(CancellationToken cancellationToken = new CancellationToken())
//     {
//         return Task.CompletedTask;
//     }
    
// }

// public class AspNetPipelineTest
// {
//     private const string Topic = "some:topic";
//     private static readonly ExamplePayload Payload = new() { Name = "some-message"};

//     private static HttpContext MakeFakeContext(string method, string path)
//     {
//         var context = new Mock<HttpContext>();
//         var request = new Mock<HttpRequest>();
//         request.Setup(x => x.Method).Returns(method);
//         request.Setup(x => x.Path).Returns(path);
//         request.Setup(x => x.Headers).Returns(new HeaderDictionary());
//         request.Setup(x => x.Query).Returns(new QueryCollection());

//         var response = new StubHttpResponse();

//         var session = new Mock<ISession>();
//         var user = new ClaimsPrincipal();
//         var identity = new Mock<IIdentity>();

//         context.Setup(c => c.Request).Returns(request.Object);
//         context.Setup(c => c.Response).Returns(response);
//         context.Setup(c => c.Session).Returns(session.Object);
//         context.Setup(c => c.User).Returns(user);
//         identity.Setup(i => i.IsAuthenticated).Returns(true);
//         identity.Setup(i => i.Name).Returns("admin");

//         return context.Object;
//     }

//     private static HttpContext CreateHttpContext()
//     {
//         return MakeFakeContext("GET", "/example");
//     }

//     [Fact]
//     public async Task Send()
//     {
//         var services = new ServiceCollection();
//         services
//             .AddTransient<ILogger<MessageRouter<AspNetContext>>>(_ => NullLogger<MessageRouter<AspNetContext>>.Instance)
//             .ConfigureServiceCollection()
//             .UsingBenzene(x => x
//                 .AddBenzene()
//                 .AddAspNetMessageHandlers()
//                 .AddMessageHandlers(GetType().Assembly)
//                 );

//         var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

//         var pipeline = new MiddlewarePipelineBuilder<AspNetContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

//         pipeline
//             .UseProcessResponse()
//             .UseMessageHandlers()
//             .Build();

//         var application = new AspNetApplication(pipeline.Build(), serviceResolverFactory);

//         var httpContext = CreateHttpContext();

//         await application.HandleAsync(httpContext);

//         Assert.NotNull(httpContext.Response);
//         Assert.Equal(200, httpContext.Response.StatusCode);
//     }

//     [Fact]
//     public async Task Send_Xml()
//     {
//         var services = new ServiceCollection();
//         services
//             .AddTransient<ILogger<MessageRouter<AspNetContext>>>(_ => NullLogger<MessageRouter<AspNetContext>>.Instance)
//             .UsingBenzene(x => x
//                 .AddBenzene()
//                 .AddMessageHandlers(GetType().Assembly)
//                 .AddHttpMessageHandlers())
//             .AddScoped<IResponseHandler<AspNetContext>, ResponseBodyHandler<AspNetContext>>()
//             .AddScoped<IResponseHandler<AspNetContext>, HttpStatusCodeResponseHandler<AspNetContext>>();

//         var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

//         var pipeline = new MiddlewarePipelineBuilder<AspNetContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

//         pipeline
//             .UseProcessResponse()
//             .UseMessageHandlers();

//         var application = new AspNetApplication(pipeline.Build(), serviceResolverFactory);

//         // var request = CreateRequest().WithHeader("content-type", "application/xml");

//         var httpContext = CreateHttpContext();

//         await application.HandleAsync(httpContext);
    
//         Assert.NotNull(httpContext.Response);
//         Assert.Equal(200, httpContext.Response.StatusCode);
//     }


//     // [Fact]
//     // public void Mapper()
//     // {
//     //     var mockHttpEndpointFinder = new Mock<IHttpEndpointFinder>();
//     //     mockHttpEndpointFinder.Setup(x => x.FindRoutes())
//     //         .Returns(new Dictionary<(string, string), string>()
//     //         {
//     //             {("GET", "example"), Topic }
//     //         });
//     //
//     //     var apiGatewayContext = new AspNetContext(CreateRequest());
//     //
//     //     var actualMessage = new JsonAspNetMessageBodyMapper(new RouteFinder(mockHttpEndpointFinder.Object)).GetBody<ExamplePayload>(apiGatewayContext);
//     //     var actualTopic = new ApiGatewayMessageTopicMapper(new RouteFinder(mockHttpEndpointFinder.Object)).GetTopic(apiGatewayContext);
//     //     var headers = new ApiGatewayMessageHeadersMapper().GetHeaders(apiGatewayContext);
//     //     var correlationId = new ApiGatewayMessageHeadersMapper().GetHeader(apiGatewayContext, "correlationId");
//     //
//     //     Assert.Equal(Payload.Name, actualMessage.Name);
//     //     Assert.Equal(Topic, actualTopic);
//     //     Assert.Single(headers);
//     //     Assert.True(headers.ContainsKey("correlationId"));
//     //     Assert.NotNull(correlationId);
//     // }
//     //
//     // [Fact]
//     // public void Mapper_MissingRoute()
//     // {
//     //     var mockHttpEndpointFinder = new Mock<IHttpEndpointFinder>();
//     //     mockHttpEndpointFinder.Setup(x => x.FindRoutes())
//     //         .Returns(new Dictionary<(string, string), string>());
//     //
//     //     var apiGatewayContext = new ApiGatewayContext(CreateRequest());
//     //
//     //     var actualMessage = new JsonApiGatewayMessageBodyMapper(new RouteFinder(mockHttpEndpointFinder.Object)).GetBody<ExamplePayload>(apiGatewayContext);
//     //     var actualTopic = new ApiGatewayMessageTopicMapper(new RouteFinder(mockHttpEndpointFinder.Object)).GetTopic(apiGatewayContext);
//     //
//     //     Assert.Null(actualMessage);
//     //     Assert.Null(actualTopic);
//     // }

//     // [Fact]
//     // public async Task Send_HealthCheck()
//     // {
//     //     var mockHealthCheck = new Mock<IHealthCheck>();
//     //     mockHealthCheck.Setup(x => x.ExecuteAsync(It.IsAny<IServiceResolver>())).ReturnsAsync(new HealthCheckResult(
//     //         true,
//     //         "example-health-check",
//     //         null
//     //     ));
//     //
//     //     var services = new ServiceCollection();
//     //     services
//     //         .AddTransient<ILogger<MessageRouter<ApiGatewayContext>>>(_ => NullLogger<MessageRouter<ApiGatewayContext>>.Instance)
//     //         .AddServiceResolver()
//     //         .AddMessageHandlers(GetType().Assembly)
//     //         .AddHttpMessageHandlers(GetType().Assembly)
//     //         .AddAwsMessageHandlers();
//     //
//     //     var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();
//     //
//     //     var pipeline = new MiddlewarePipeline<ApiGatewayContext>();
//     //
//     //     pipeline
//     //         .UseProcessResponse()
//     //         .UseHealthCheck("healthcheck", "GET", "/healthcheck", new IHealthCheck[]
//     //         {
//     //             mockHealthCheck.Object
//     //         })
//     //         .UseMessageHandlers();
//     //
//     //     var aws = new ApiGatewayApplication(pipeline);
//     //
//     //     var request = AwsEventBuilder.CreateApiGatewayEvent("GET", "/healthcheck", null);
//     //
//     //     var response = await aws.HandleAsync(request, serviceResolver);
//     //
//     //     Assert.NotNull(response);
//     // }
// }
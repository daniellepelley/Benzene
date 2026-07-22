using Amazon.Lambda.RuntimeSupport;
using Benzene.Examples.AwsMesh.Analytics;

// Custom-runtime (provided.al2023) bootstrap: .NET 10 has no managed Lambda runtime, so the function
// ships as a self-contained executable that hosts the Benzene pipeline and pumps invocations.
var function = new Function();
using var handlerWrapper = HandlerWrapper.GetHandlerWrapper(function.FunctionHandlerAsync);
using var bootstrap = new LambdaBootstrap(handlerWrapper);
await bootstrap.RunAsync();

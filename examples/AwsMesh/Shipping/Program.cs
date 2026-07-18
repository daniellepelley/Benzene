using Amazon.Lambda.RuntimeSupport;
using Benzene.Examples.AwsMesh.Shipping;

// Custom-runtime (provided.al2023) bootstrap — see the Orders service for the rationale.
var function = new Function();
using var handlerWrapper = HandlerWrapper.GetHandlerWrapper(function.FunctionHandlerAsync);
using var bootstrap = new LambdaBootstrap(handlerWrapper);
await bootstrap.RunAsync();

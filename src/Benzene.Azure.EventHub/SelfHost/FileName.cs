// using System.Text;
// using System.Text.Json;
// using Azure.Messaging.EventHubs;
// using Azure.Messaging.EventHubs.Processor;
// using Azure.Messaging.EventHubs.Producer;
// using Benzene.Abstractions.Logging;
// using Benzene.Abstractions.MessageHandlers;
// using Microsoft.Extensions.Azure;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
//
// namespace Benzene.Azure.EventHub.SelfHost;
//
// public class StreamProcessor<TMessage, THandler> : IHostedService
//     where THandler : IMessageHandler<TMessage>
// {
//     private readonly EventProcessorClient _eventHubProcessorClient;
//     private readonly IAzureClientFactory<EventHubProducerClient> _eventProducerClientFactory;
//     private readonly JsonSerializerOptions? _jsonSerializerOptions;
//
//     private readonly IBenzeneLogger _logger;
//     private readonly string _configKey;
//     private readonly IServiceProvider _services;
//
//     public StreamProcessor(
//         IBenzeneLogger logger,
//         IAzureClientFactory<EventProcessorClient> eventProcessorClientFactory,
//         IAzureClientFactory<EventHubProducerClient> eventProducerClientFactory,
//         IServiceProvider services,
//         string configKey,
//         JsonSerializerOptions? jsonSerializerOptions = null)
//     {
//         _logger = logger;
//         _eventHubProcessorClient = eventProcessorClientFactory.CreateClient(configKey);
//         _services = services;
//         _jsonSerializerOptions = jsonSerializerOptions;
//         _eventProducerClientFactory = eventProducerClientFactory;
//
//         _configKey = configKey;
//     }
//
//     async Task IHostedService.StartAsync(CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("StreamProcessor<{}> running. ", _configKey);
//
//         await StartProcessor(cancellationToken);
//     }
//
//     async Task IHostedService.StopAsync(CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("StreamProcessor<{}> stopping. ", _configKey);
//
//         await StopProcessor(cancellationToken);
//     }
//
//     private async Task ProcessMessageHandler(ProcessEventArgs eventArgs)
//     {
//         try
//         {
//             _logger.LogInformation("Received message: SequenceNumber: {}", eventArgs.Data.SequenceNumber);
//
//             if (!eventArgs.HasEvent)
//             {
//                 return;
//             }
//
//             if (!TryDeserialize(eventArgs, out var @event))
//             {
//                 //Update event checkpoint so we dont constantly cycle through it.
//                 await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
//
//                 return;
//             }
//
//             await Handle(eventArgs, @event!, eventArgs.CancellationToken);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Fatal exception stopping processor : {}", ex.Message);
//
//             // Microsoft recommendation to terminate the process and let orchestration restart
//             Environment.Exit(-1);
//         }
//     }
//
//     private async Task Handle(ProcessEventArgs eventArgs, TMessage @event, CancellationToken cancellationToken)
//     {
//         
//         var retryCount = 0;
//
//         while (retryCount < _messageHandlerOptions.ErrorHandlingOptions.RetryAttempts)
//         {
//             using var operation = StartOperation(@event.DistributedTraceCorrelationId, retryCount);
//
//             var handledSuccessfully = await HandleMessage(@event, eventArgs.CancellationToken);
//
//             if (handledSuccessfully)
//             {
//                 return;
//             }
//
//             retryCount += 1;
//
//             operation.Telemetry.Success = false;
//             operation.Telemetry.ResponseCode = "500";
//
//             if (retryCount >= _messageHandlerOptions.ErrorHandlingOptions.RetryAttempts)
//             {
//                 await SendToErrorTopic(eventArgs.Data);
//
//                 //Update event checkpoint so we dont constantly cycle through it.
//                 await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
//
//                 return;
//             }
//
//             await Task.Delay(TimeSpan.FromSeconds(retryCount * retryCount), cancellationToken);
//
//             _logger.LogWarning("Event {} Handling Retry {}", typeof(TMessage).Name, retryCount);
//         }
//     }
//
//     private async Task<bool> HandleMessage(TMessage @event, CancellationToken cancellationToken)
//     {
//         try
//         {
//             using var scope = _services.CreateScope();
//             var handler = scope.ServiceProvider.GetRequiredKeyedService<THandler>(_configKey);
//
//             await handler.Handle(@event, cancellationToken);
//
//             return true;
//         }
//         catch (InvalidOperationException)
//         {
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "Unhandled Exception {}, {} ({})", e.Message, typeof(TMessage).Name, _configKey);
//
//             return false;
//         }
//     }
//
//     private Task ProcessErrorHandler(ProcessErrorEventArgs arg)
//     {
//         _logger.LogError(arg.Exception, "EventProcessorClient exception {}", arg.Exception.Message);
//
//         return Task.CompletedTask;
//     }
//
//     private async Task StartProcessor(CancellationToken cancellationToken)
//     {
//         if (IsAlreadyRunning())
//         {
//             _logger.LogWarning("StreamProcessor<{}> ({}) already running stopping. ", typeof(TMessage).Name, _configKey);
//             return;
//         }
//
//         _eventHubProcessorClient.ProcessEventAsync += ProcessMessageHandler;
//         _eventHubProcessorClient.ProcessErrorAsync += ProcessErrorHandler;
//
//         await _eventHubProcessorClient.StartProcessingAsync(cancellationToken);
//     }
//
//     private async Task StopProcessor(CancellationToken cancellationToken)
//     {
//         await _eventHubProcessorClient.StopProcessingAsync(cancellationToken);
//
//         try
//         {
//             _eventHubProcessorClient.ProcessEventAsync -= ProcessMessageHandler;
//         }
//         catch (ArgumentException)
//         {
//             // Swallowing : some exceptional circumstances the handler has not been assigned to the event
//         }
//
//         try
//         {
//             _eventHubProcessorClient.ProcessErrorAsync -= ProcessErrorHandler;
//         }
//         catch (ArgumentException)
//         {
//             // Swallowing : some exceptional circumstances the handler has not been assigned to the event
//         }
//     }
//
//     private bool TryDeserialize(ProcessEventArgs args, out TMessage? @event)
//     {
//         try
//         {
//             @event = JsonSerializer.Deserialize<TMessage>(
//                 Encoding.UTF8.GetString(args.Data.EventBody.ToArray()), _jsonSerializerOptions);
//
//             if (@event is null)
//             {
//                 _logger.LogWarning("failed to deserialize event {} ({})", typeof(TMessage).Name, _configKey);
//                 return false;
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Unhandled Exception {}, {} ({})", ex.Message, typeof(TMessage).Name, _configKey);
//             @event = default;
//
//             return false;
//         }
//
//         return true;
//     }
//
//     private bool IsAlreadyRunning() => _eventHubProcessorClient.IsRunning;
// }
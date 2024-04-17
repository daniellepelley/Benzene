// using Benzene.Abstractions.DI;
// using Benzene.Abstractions.MessageHandling;
// using Benzene.Abstractions.Results;
// using Benzene.Core.Results;
//
// namespace Benzene.HealthChecks;
//
// public class HealthCheckMessageHandler : IMessageHandler<NullPayload, HealthCheckResult>
// {
//     private readonly IEnumerable<IHealthCheck> _healthChecks;
//     private readonly IServiceResolver _serviceResolver;
//
//     public HealthCheckMessageHandler(IEnumerable<IHealthCheck> healthChecks, IServiceResolver serviceResolver)
//     {
//         _healthChecks = healthChecks;
//         _serviceResolver = serviceResolver;
//     }
//
//     public async Task<IServiceResult<HealthCheckResult>> HandleAsync(NullPayload request)
//     {
//         var tasks = _healthChecks.Select(x => x.ExecuteAsync(_serviceResolver)).ToArray();
//
//         var results = await Task.WhenAll(tasks);
//
//         var success = results
//             .All(x => x.Status != HealthCheckStatus.Failed);
//
//         var data = results.ToDictionary(x => x.Type, x => new { x.Status, x.Data } as object);
//
//         return ServiceResult.Ok(new HealthCheckResult(success, string.Empty, data));
//     }
// }

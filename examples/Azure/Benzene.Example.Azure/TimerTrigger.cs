// using System;
// using System.Threading.Tasks;
// using Microsoft.Azure.WebJobs;
// using Microsoft.Extensions.Logging;
//
// namespace Benzene.Example.Azure;
//
// public static class TimerTrigger
// {
//     [FunctionName("TimerTrigger")]
//     public static async Task RunAsync([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
//     {
//         log.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}");
//     }
// }
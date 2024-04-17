// using System.Collections.Generic;
// using System.Dynamic;
// using Xunit;
// using HealthCheckResponse = Benzene.Clients.HealthChecks.HealthCheckResponse;
//
// namespace Benzene.Test.Clients;
//
// public class HealthCheckProcessorTest
// {
//     [Fact]
//     public void Process()
//     {
//         dynamic data = new ExpandoObject();
//         data.hashCode = "some-code";
//
//         var healthCheckResult = new HealthCheckResponse(true,
//             new Dictionary<string, Benzene.Clients.HealthChecks.HealthCheckResult>
//             {
//                 { "schema", new Benzene.Clients.HealthChecks.HealthCheckResult(
//             "ok",
//             "some-type",
//             new Dictionary<string, object>
//             {
//                { "data", data }
//             }
//             )
//         }});
//
//         var result = Benzene.Clients.HealthChecks.HealthCheckProcessor.Process(healthCheckResult, "some-code");
//         dynamic output = result.HealthChecks["schema"].Data["data"];
//
//         Assert.True(result.IsHealthy);
//         Assert.True(output.isMatch);
//     }
// }

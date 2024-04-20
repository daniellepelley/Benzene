// using System;
// using System.Threading.Tasks;
// using Benzene.Examples.Google.Tests.Helpers;
//
// namespace Benzene.Examples.Google.Tests.Integration;
//
// public abstract class IntegrationTestBase : IDisposable
// {
//     public IntegrationTestBase()
//     {
//         SetUpAsync().Wait();
//     }
//
//     public void Dispose()
//     {
//         TearDownAsync().Wait();
//     }
//
//     private async Task SetUpAsync()
//     {
//         EnvironmentSetUp.SetUp();
//         await Task.WhenAll(
//             DatabaseSetup.ResetDatabaseAsync());
//     }
//
//     private async Task TearDownAsync()
//     {
//         await Task.CompletedTask;
//     }
// }
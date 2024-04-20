// using System.Threading.Tasks;
// using Benzene.Examples.Aws.Data;
// using Microsoft.EntityFrameworkCore;
//
// namespace Benzene.Examples.Aws.Tests.Helpers;
//
// public static class DatabaseSetup
// {
//     public static string ConnectionString
//     {
//         get
//         {
//             var connectionString = DependenciesBuilder.GetConfiguration()["DB_CONNECTION_STRING"];
//             return connectionString;
//         }
//     }
//
//     public static DataContext CreateDataContext()
//     {
//         var dataContext = new DataContext(new DbContextOptionsBuilder<DataContext>().UseNpgsql(ConnectionString).Options);
//         return dataContext;
//     }
//
//     public static DataContext CreateDataContext(string connectionString)
//     {
//         var dataContext = new DataContext(new DbContextOptionsBuilder<DataContext>().UseNpgsql(connectionString).Options);
//         return dataContext;
//     }
//
//
//     public static async Task ResetDatabaseAsync()
//     {
//         using (var dataContext = CreateDataContext())
//         {
//             await dataContext.Database.EnsureDeletedAsync();
//             await dataContext.Database.EnsureCreatedAsync();
//         }
//     }
// }
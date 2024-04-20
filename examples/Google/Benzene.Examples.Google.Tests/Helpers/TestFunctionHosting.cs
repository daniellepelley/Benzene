using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Benzene.Examples.Google.Tests.Helpers;

public static class TestFunctionHosting
{
    public static async Task SendHttpContextAsync(HttpContext httpContext)
    {
        var entryPoint = FunctionFactory.Create();
        await entryPoint.HandleAsync(httpContext);
    }
}
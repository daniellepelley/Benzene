using System.Threading.Tasks;
using Benzene.GoogleCloud.Functions.Http.TestHelpers;
using Microsoft.AspNetCore.Http;

namespace Benzene.Examples.Google.Tests.Helpers;

public static class TestFunctionHosting
{
    public static Task SendHttpContextAsync(HttpContext httpContext)
    {
        var function = FunctionFactory.Create();
        return function.SendHttpAsync(httpContext);
    }
}

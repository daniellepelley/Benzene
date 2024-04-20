using System;

namespace Benzene.Examples.Google.Tests.Helpers;

public static class FunctionFactory
{
    public static HttpFunction Create()
    {
        Environment.SetEnvironmentVariable("FT_SERVICE_LIVE", "TRUE");
        return new HttpFunction();
    }
}
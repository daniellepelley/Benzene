using Benzene.Examples.Google;
using Benzene.GoogleCloud.Functions.Http.TestHelpers;
using Benzene.Testing;
using Google.Cloud.Functions.Framework;

namespace Benzene.Examples.Google.Tests.Helpers;

public static class FunctionFactory
{
    public static IHttpFunction Create()
    {
        return BenzeneTestHost.Create<Startup>().BuildGoogleCloudFunctionHost();
    }
}

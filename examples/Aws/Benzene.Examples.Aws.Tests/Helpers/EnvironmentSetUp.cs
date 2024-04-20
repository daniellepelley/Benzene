using System;

namespace Benzene.Examples.Aws.Tests.Helpers;

public static class EnvironmentSetUp
{
    public static void SetUp()
    {
        Environment.SetEnvironmentVariable("AWS_SERVICE_URL", "http://localhost:4566");
        Environment.SetEnvironmentVariable("MY_QUEUE_URL", "http://localhost:4566/245633934812/my-queue");
    }
}
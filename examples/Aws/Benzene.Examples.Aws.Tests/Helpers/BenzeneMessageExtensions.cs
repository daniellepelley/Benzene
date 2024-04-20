using System;
using Benzene.Core.BenzeneMessage;
using Newtonsoft.Json;

namespace Benzene.Examples.Aws.Tests.Helpers;

public static class BenzeneMessageExtensions
{
    public static T GetMessage<T>(this BenzeneMessageResponse source)
    {
        return JsonConvert.DeserializeObject<T>(source.Body);
    }

    public static bool BodyIsGuid(this BenzeneMessageResponse source)
    {
        return Guid.TryParse(source.Body.Replace(@"""", ""), out _);
    }
}
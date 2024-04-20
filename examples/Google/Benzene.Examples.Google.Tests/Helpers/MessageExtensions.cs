using Benzene.Core.Helper;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Benzene.Examples.Google.Tests.Helpers;

public static class MessageExtensions
{
    public static T Body<T>(this HttpResponse httpResponse)
    {
        return JsonConvert.DeserializeObject<T>(Utils.StreamToString(httpResponse.Body));
    }
}
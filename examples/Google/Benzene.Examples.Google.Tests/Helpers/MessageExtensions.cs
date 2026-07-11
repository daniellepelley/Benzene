using System.IO;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Benzene.Examples.Google.Tests.Helpers;

public static class MessageExtensions
{
    public static T Body<T>(this HttpResponse httpResponse)
    {
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpResponse.Body);
        return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
    }
}

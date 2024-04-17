using System.Text;
using Newtonsoft.Json;

namespace Benzene.Tools;

public static class Utils
{
    public static string StreamToString(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public static MemoryStream StringToStream(string src)
    {
        var byteArray = Encoding.UTF8.GetBytes(src);
        return new MemoryStream(byteArray);
    }

    public static MemoryStream ObjectToStream(object obj)
    {
        return StringToStream(JsonConvert.SerializeObject(obj));
    }
}

using Benzene.Core;

namespace Benzene.Xml;

public static class Settings
{
    public static string ContentTypeKey { get; set; } = Core.Constants.ContentTypeHeader;
    public static string ContentTypeValue { get; set; } = Constants.XmlContentType;
}
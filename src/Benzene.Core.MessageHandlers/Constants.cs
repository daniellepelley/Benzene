using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Core.MessageHandlers
{
    public static class Constants
    {
        public static ITopic Missing => new Topic("<missing>");
        public const string ContentTypeHeader = "content-type";
        public const string JsonContentType = "application/json";
    }
}

using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.Mappers;

namespace Benzene.Core.MessageHandlers
{
    public static class Constants
    {
        public static ITopic Missing => new Topic("<missing>");
    }
}

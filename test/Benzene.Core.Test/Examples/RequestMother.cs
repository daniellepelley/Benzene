
using Benzene.Tools;

namespace Benzene.Test.Examples;

public static class RequestMother
{
    public static MessageBuilder CreateExampleEvent()
    {
        return MessageBuilder.Create(Defaults.Topic,
            new ExampleRequestPayload { Id = Defaults.Id, Name = Defaults.Name, });
    }

    public static HttpBuilder CreateExampleHttp()
    {
        return HttpBuilder.Create(Defaults.Method, Defaults.Path,
            new ExampleRequestPayload { Id = Defaults.Id, Name = Defaults.Name, });
    }

    public static MessageBuilder CreateSerializationErrorPayload()
    {
        return MessageBuilder.Create(Defaults.TopicWithGuid,
            new { Id = ""});
    }
}

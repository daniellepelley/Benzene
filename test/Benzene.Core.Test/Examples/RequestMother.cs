using Benzene.Testing;

namespace Benzene.Test.Examples;

public static class RequestMother
{
    public static MessageBuilder<ExampleRequestPayload> CreateExampleEvent()
    {
        return MessageBuilder.Create(Defaults.Topic,
            new ExampleRequestPayload { Id = Defaults.Id, Name = Defaults.Name, });
    }

    public static HttpBuilder<ExampleRequestPayload> CreateExampleHttp()
    {
        return HttpBuilder.Create<ExampleRequestPayload>(Defaults.Method, Defaults.Path,
            new ExampleRequestPayload { Id = Defaults.Id, Name = Defaults.Name, });
    }

    public static MessageBuilder<object> CreateSerializationErrorPayload()
    {
        return MessageBuilder.Create<object>(Defaults.TopicWithGuid,
            new { Id = ""});
    }
}

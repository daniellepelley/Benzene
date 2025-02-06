using System;
using System.Collections.Generic;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Clients.Aws.Sqs;

public class GetTopicTest
{
    [Fact]
    public void DictionaryGetTopic()
    {
        var dictionaryGetTopic = new DictionaryGetTopic(new Dictionary<Type, string>
        {
            { typeof(ExampleRequestPayload), Defaults.Topic }
        });

        var topic = dictionaryGetTopic.GetTopic(typeof(ExampleRequestPayload));

        Assert.Equal(Defaults.Topic, topic);
    }
}
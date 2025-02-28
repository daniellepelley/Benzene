using System;
using System.Collections.Generic;
using Benzene.Abstractions.Messages.BenzeneClient;

namespace Benzene.Test.Clients.Aws.Sqs;

public class DictionaryGetTopic : IGetTopic
{
    private readonly IDictionary<Type, string> _dictionary;

    public DictionaryGetTopic(IDictionary<Type, string> dictionary)
    {
        _dictionary = dictionary;
    }

    public string GetTopic(Type type)
    {
        return _dictionary[type];
    }
}
using System;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.SQS.Model;
using Newtonsoft.Json;

namespace Benzene.Examples.Aws.Tests.Helpers;

public static class MessageExtensions
{
    public static string GetTopic(this Message message)
    {
        return message.GetHeader("topic");
    }

    public static string GetStatus(this Message message)
    {
        return message.GetHeader("status");
    }

    public static bool BodyIsGuid(this Message message)
    {
        return Guid.TryParse(message.Body.Replace(@"""", ""), out _);
    }

    public static bool BodyContains(this Message message, string text)
    {
        return message.Body.Contains(text);
    }

    public static T Body<T>(this Message message)
    {
        return JsonConvert.DeserializeObject<T>(message.Body);
    }

    public static T Body<T>(this APIGatewayProxyResponse message)
    {
        return JsonConvert.DeserializeObject<T>(message.Body);
    }

    private static string GetHeader(this Message message, string key)
    {
        return message.MessageAttributes.ContainsKey(key)
            ? message.MessageAttributes[key].StringValue
            : null;
    }
}
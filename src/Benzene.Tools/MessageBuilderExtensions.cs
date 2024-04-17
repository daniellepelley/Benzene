using System.Text;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.KafkaEvents;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS.Model;
using Benzene.Core.DirectMessage;
using Newtonsoft.Json;

namespace Benzene.Tools;

public static class MessageBuilderExtensions
{
    public static MessageBuilder WithHeaders(this MessageBuilder source, IDictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            source.WithHeader(header.Key, header.Value);
        }

        return source;
    }

    public static DirectMessageRequest AsDirectMessage(this MessageBuilder source)
    {
        return new DirectMessageRequest
        {
            Topic = source.Topic,
            Message = JsonConvert.SerializeObject(source.Message),
            Headers = source.Headers
        };
    }

    public static SNSEvent AsSns(this MessageBuilder source)
    {
        var headers = source.Headers.ToDictionary(x => x.Key, x => x.Value);
        headers.Add("topic", source.Topic);
        return new SNSEvent
        {
            Records = new List<SNSEvent.SNSRecord>
            {
                new SNSEvent.SNSRecord
                {
                    EventSource = "aws:sns",
                    Sns = new SNSEvent.SNSMessage
                    {
                        MessageAttributes = headers.ToDictionary(x => x.Key, x => new SNSEvent.MessageAttribute
                                {
                                    Value = x.Value,
                                    Type = "String"
                                }),
                        Message = JsonConvert.SerializeObject(source.Message)
                    }
                }
            }
        };
    }

    public static SNSEvent AsEventBusSns(this MessageBuilder source)
    {
        return new SNSEvent
        {
            Records = new List<SNSEvent.SNSRecord>
            {
                new SNSEvent.SNSRecord
                {
                    EventSource = "aws:sns",
                    Sns = new SNSEvent.SNSMessage
                    {
                        MessageAttributes = new Dictionary<string, SNSEvent.MessageAttribute>
                        {
                            {
                                "topic", new SNSEvent.MessageAttribute
                                {
                                    Value = source.Topic,
                                    Type = "String"
                                }
                            },
                            {
                                "sender", new SNSEvent.MessageAttribute
                                {
                                    Value = Guid.NewGuid().ToString(),
                                    Type = "String"
                                }
                            },
                            {
                                "correlationId", new SNSEvent.MessageAttribute
                                {
                                    Value = Guid.NewGuid().ToString(),
                                    Type = "String"
                                }
                            },
                            {
                                 "trace", new SNSEvent.MessageAttribute
                                 {
                                     Value =
                                         "[{  \"sent\": \"2021-06-20T15:38:27.966443Z\",    \"received\": \"0001-01-01T00:00:00\",    \"sender\": \"platform-eventbus-example-dotnet-svc\",    \"receiver\": null,    \"channel\": \"SQS\",    \"elapse\": \"n/a\"}]"
                                }
                            }
                        }
                        .Concat(source.Headers.ToDictionary(x => x.Key, x=> new SNSEvent.MessageAttribute
                        {
                            Value = x.Value,
                            Type = "String"
                        } ))
                        .ToDictionary(x => x.Key, x => x.Value) ,
                        Message = JsonConvert.SerializeObject(source.Message)
                    }
                }
            }
        };
    }

    public static SQSEvent AsSqs(this MessageBuilder source, int numberOfMessages = 1)
    {
        var headers = source.Headers.ToDictionary(x => x.Key, x => x.Value);
        headers.Add("topic", source.Topic);

        return new SQSEvent
        {
            Records = Enumerable.Range(0, numberOfMessages).Select(_ =>
                new SQSEvent.SQSMessage
                {
                    MessageId = Guid.NewGuid().ToString(),
                    EventSource = "aws:sqs",
                    MessageAttributes = headers.ToDictionary(x => x.Key, x => new SQSEvent.MessageAttribute
                    {
                        StringValue = x.Value,
                        DataType = "String"
                    }),
                    Body = JsonConvert.SerializeObject(source.Message)
                }
            ).ToList()
        };
    }

    public static Message AsSqsMessage(this MessageBuilder source)
    {
        var headers = source.Headers.ToDictionary(x => x.Key, x => x.Value);
        headers.Add("topic", source.Topic);

        return new Message
        {
            MessageAttributes = headers.ToDictionary(x => x.Key, x => new MessageAttributeValue
            {
                StringValue = x.Value,
                DataType = "String"
            }),
            Body = JsonConvert.SerializeObject(source.Message)
        };
    }

    public static SQSEvent AsEventBusSqs(this MessageBuilder source)
    {
        return new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    EventSource = "aws:sqs",
                    MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
                    {
                        {
                            "topic", new SQSEvent.MessageAttribute
                            {
                                StringValue = source.Topic,
                                DataType = "String"
                            }
                        },
                        {
                            "sender", new SQSEvent.MessageAttribute
                            {
                                StringValue = Guid.NewGuid().ToString(),
                                DataType = "String"
                            }
                        },
                        {
                            "correlationId", new SQSEvent.MessageAttribute
                            {
                                StringValue = Guid.NewGuid().ToString(),
                                DataType = "String"
                            }
                        },
                        {
                            "trace", new SQSEvent.MessageAttribute
                            {
                                StringValue =
                                    "[{  \"sent\": \"2021-06-20T15:38:27.966443Z\",    \"received\": \"0001-01-01T00:00:00\",    \"sender\": \"platform-eventbus-example-dotnet-svc\",    \"receiver\": null,    \"channel\": \"SQS\",    \"elapse\": \"n/a\"}]"
                            }
                        }
                    },
                    Body = JsonConvert.SerializeObject(source.Message)
                }
            }
        };
    }

    public static KafkaEvent AsAwsKafkaEvent(this MessageBuilder source)
    {
        return new KafkaEvent
        {
            EventSource = "aws:kafka",
            Records = new Dictionary<string, IList<KafkaEvent.KafkaEventRecord>>
            {
                {
                    "some-id",
                    new List<KafkaEvent.KafkaEventRecord>
                    {
                        new KafkaEvent.KafkaEventRecord { Topic = source.Topic, Value = Utils.ObjectToStream(source.Message) }
                    }
                }
            }
        };
    }

    public static APIGatewayProxyRequest AsApiGatewayRequest(this HttpBuilder source)
    {
        return new APIGatewayProxyRequest
        {
            HttpMethod = source.Method,
            Path = source.Path,
            Body = JsonConvert.SerializeObject(source.Message),
            Headers = source.Headers
        };
    }

    public static string AsRawHttpRequest(this HttpBuilder source)
    {
        var stringBuilder = new StringBuilder();
        // Start line
        stringBuilder.AppendLine($"{source.Method} {source.Path} HTTP/1.1");
        // Headers
        foreach (var header in source.Headers)
        {
            stringBuilder.AppendLine($"{header.Key}: {header.Value}");
        }

        // Empty line to separate headers and body
        stringBuilder.AppendLine();
        // Body
        var body = JsonConvert.SerializeObject(source.Message);
        stringBuilder.Append(body);
        return stringBuilder.ToString();
    }

    public static APIGatewayCustomAuthorizerRequest AsApiGatewayCustomAuthorizerEvent(this HttpBuilder source)
    {
        return new APIGatewayCustomAuthorizerRequest
        {
            HttpMethod = source.Method,
            Path = source.Path,
            Headers = new Dictionary<string, string>
            {
                { "x-correlation-id", Guid.NewGuid().ToString() }
            },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                ApiId = "some-id"
            }
        };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json;

namespace Benzene.Examples.Aws.Tests.Helpers;

public class AwsEventBuilder
{
    private string _body;
    private readonly IDictionary<string, string> _headers = new Dictionary<string, string>();
    private string _topic;

    public AwsEventBuilder WithTopic(string topic)
    {
        _topic = topic;
        return this;
    }

    public AwsEventBuilder WithJsonBody(object body)
    {
        _body = JsonConvert.SerializeObject(body);
        ;
        return this;
    }

    public AwsEventBuilder WithXmlBody<T>(T body)
    {
        _headers.Add("content-type", "application/xml");
        _body = XmlHelper.ToXml(body);
        return this;
    }

    public SNSEvent CreateSnsEvent()
    {
        return new SNSEvent
        {
            Records = new List<SNSEvent.SNSRecord>
            {
                new()
                {
                    EventSource = "aws:sns",
                    Sns = new SNSEvent.SNSMessage
                    {
                        MessageAttributes = new Dictionary<string, SNSEvent.MessageAttribute>
                        {
                            {
                                "topic", new SNSEvent.MessageAttribute
                                {
                                    Value = _topic,
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
                        }.Concat(_headers.Select(x => new KeyValuePair<string, SNSEvent.MessageAttribute>(x.Key,
                            new SNSEvent.MessageAttribute
                            {
                                Type = "string",
                                Value = x.Value
                            }))).ToDictionary(x => x.Key, x => x.Value),
                        Message = _body
                    }
                }
            }
        };
    }

    public SQSEvent CreateSqsEvent()
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
                                StringValue = _topic,
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
                                    "[{  \"sent\": \"2021-06-20T15:38:27.966443Z\",    \"received\": \"0001-01-01T00:00:00\",    \"sender\": \"platform-eventbus-example-dotnet-svc\",    \"receiver\": null,    \"channel\": \"SQS\",    \"elapse\": \"n/a\"}]",
                                DataType = "String"

                            }
                        }

                    }.Concat(_headers.Select(x => new KeyValuePair<string, SQSEvent.MessageAttribute>(x.Key,
                        new SQSEvent.MessageAttribute
                        {
                            StringValue = x.Value,
                            DataType = "String"
                        }))).ToDictionary(x => x.Key, x => x.Value),
                    Body = _body
                }
            }
        };
    }

    public static SNSEvent CreateSnsEvent(string topic, object message)
    {
        
        return new AwsEventBuilder()
            .WithTopic(topic)
            .WithJsonBody(message)
            .CreateSnsEvent();
        // return CreateSnsEventFromString(topic,
        // JsonConvert.SerializeObject(message));
    }

    public static SNSEvent CreateSnsEventFromString(string topic, string message)
    {
        return new AwsEventBuilder()
            .WithTopic(topic)
            .WithJsonBody(message)
            .CreateSnsEvent();
        return new SNSEvent
        {
            Records = new List<SNSEvent.SNSRecord>
            {
                new()
                {
                    EventSource = "aws:sns",
                    Sns = new SNSEvent.SNSMessage
                    {
                        MessageAttributes = new Dictionary<string, SNSEvent.MessageAttribute>
                        {
                            {
                                "topic", new SNSEvent.MessageAttribute
                                {
                                    Value = topic,
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
                        },
                        Message = message
                    }
                }
            }
        };
    }

    public static SQSEvent CreateSqsEvent(string topic, object message)
    {
        return new AwsEventBuilder()
            .WithTopic(topic)
            .WithJsonBody(message)
            .CreateSqsEvent();
        
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
                                StringValue = topic,
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
                    Body = JsonConvert.SerializeObject(message)
                }
            }
        };
    }
}
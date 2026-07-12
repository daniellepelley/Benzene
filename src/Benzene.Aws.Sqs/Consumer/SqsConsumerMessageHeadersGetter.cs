using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Extracts headers from an SQS message's string-typed message attributes.
/// </summary>
public class SqsConsumerMessageHeadersGetter : IMessageHeadersGetter<SqsConsumerMessageContext>
{
    /// <summary>
    /// Gets the string-typed message attributes as headers.
    /// </summary>
    /// <param name="context">The SQS consumer message context to extract headers from.</param>
    /// <returns>A dictionary of header names to values, limited to attributes with a <c>String</c> data type.</returns>
    public IDictionary<string, string> GetHeaders(SqsConsumerMessageContext context)
    {
        return context.Message.MessageAttributes
            .Where(x => x.Value.DataType == "String")
            .ToDictionary(x => x.Key, x => x.Value.StringValue);
    }
}

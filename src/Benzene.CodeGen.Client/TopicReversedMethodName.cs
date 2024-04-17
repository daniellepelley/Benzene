using Benzene.CodeGen.Core;
using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Client;

public class TopicReversedMethodName : IMethodName
{
    public string Create(string topic, OpenApiSchema requestSchema)
    {
        return string.Join("", topic.Split(":")
            .Reverse()
            .Select(x => new FormatString(x)
                .RemoveNonIdentifierCharacters()
                .Pascalcase()
                .EnsureStartsWithLetterOrUnderScore()
            ));
    }
}

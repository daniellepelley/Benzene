using Benzene.CodeGen.Core;
using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Client;

public class TopicMethodName : IMethodName
{
    public string Create(string topic, OpenApiSchema requestSchema)
    {
        return string.Join("", topic.Split(":")
            .Select(x => new FormatString(x)
                .RemoveNonIdentifierCharacters()
                .Pascalcase()
                .EnsureStartsWithLetterOrUnderScore()
            ));
    }
}

using Benzene.Results;

namespace Benzene.Http;

public class DefaultHttpStatusCodeMapper : IHttpStatusCodeMapper
{
    private const string DefaultValue = "500";
    private readonly IDictionary<string, string> _dictionary;

    public DefaultHttpStatusCodeMapper()
    {
        _dictionary = new Dictionary<string, string>
        {
            { BenzeneResultStatus.Ok, "200"},
            { BenzeneResultStatus.Ignored, "200"},
            { BenzeneResultStatus.Created, "201"},
            { BenzeneResultStatus.Accepted, "202" },
            { BenzeneResultStatus.Updated, "204"},
            { BenzeneResultStatus.Deleted, "204"},
            { BenzeneResultStatus.BadRequest, "400"},
            { BenzeneResultStatus.Unauthorized, "401"},
            { BenzeneResultStatus.Forbidden, "403"},
            { BenzeneResultStatus.NotFound, "404"},
            { BenzeneResultStatus.Conflict, "409"},
            { BenzeneResultStatus.ValidationError, "422"},
            { BenzeneResultStatus.UnexpectedError, "500"},
            { BenzeneResultStatus.NotImplemented, "501"},
            { BenzeneResultStatus.ServiceUnavailable, "503"}
        };
    }

    public string Map(string? benzeneResultStatus)
    {
        if (benzeneResultStatus == null)
        {
            return DefaultValue;
        }

        return _dictionary.TryGetValue(benzeneResultStatus, out var map)
            ? map
            : DefaultValue;
    }
}

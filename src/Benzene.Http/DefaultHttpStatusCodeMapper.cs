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
            { ServiceResultStatus.Ok, "200"},
            { ServiceResultStatus.Ignored, "200"},
            { ServiceResultStatus.Created, "201"},
            { ServiceResultStatus.Accepted, "202" },
            { ServiceResultStatus.Updated, "204"},
            { ServiceResultStatus.Deleted, "204"},
            { ServiceResultStatus.BadRequest, "400"},
            { ServiceResultStatus.Unauthorized, "401"},
            { ServiceResultStatus.Forbidden, "403"},
            { ServiceResultStatus.NotFound, "404"},
            { ServiceResultStatus.Conflict, "409"},
            { ServiceResultStatus.ValidationError, "422"},
            { ServiceResultStatus.UnexpectedError, "500"},
            { ServiceResultStatus.NotImplemented, "501"},
            { ServiceResultStatus.ServiceUnavailable, "503"}
        };
    }

    public string Map(string? serviceResultStatus)
    {
        if (serviceResultStatus == null)
        {
            return DefaultValue;
        }

        return _dictionary.TryGetValue(serviceResultStatus, out var map)
            ? map
            : DefaultValue;
    }
}

using Benzene.Results;

namespace Benzene.Http;

/// <summary>
/// Provides the default mapping between Benzene result status codes and HTTP status codes.
/// </summary>
/// <remarks>
/// This mapper implements standard RESTful conventions for HTTP status codes, mapping
/// Benzene's domain result statuses to appropriate HTTP response codes. Unknown or null
/// status values default to 500 (Internal Server Error).
/// </remarks>
public class DefaultHttpStatusCodeMapper : IHttpStatusCodeMapper
{
    private const string DefaultValue = "500";
    private readonly IDictionary<string, string> _dictionary;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultHttpStatusCodeMapper"/> class with standard mappings.
    /// </summary>
    /// <remarks>
    /// The following mappings are configured:
    /// <list type="bullet">
    /// <item><description>Ok, Ignored → 200 (OK)</description></item>
    /// <item><description>Created → 201 (Created)</description></item>
    /// <item><description>Accepted → 202 (Accepted)</description></item>
    /// <item><description>Updated, Deleted → 204 (No Content)</description></item>
    /// <item><description>BadRequest → 400 (Bad Request)</description></item>
    /// <item><description>Unauthorized → 401 (Unauthorized)</description></item>
    /// <item><description>Forbidden → 403 (Forbidden)</description></item>
    /// <item><description>NotFound → 404 (Not Found)</description></item>
    /// <item><description>Conflict → 409 (Conflict)</description></item>
    /// <item><description>ValidationError → 422 (Unprocessable Entity)</description></item>
    /// <item><description>TooManyRequests → 429 (Too Many Requests)</description></item>
    /// <item><description>UnexpectedError → 500 (Internal Server Error)</description></item>
    /// <item><description>NotImplemented → 501 (Not Implemented)</description></item>
    /// <item><description>ServiceUnavailable → 503 (Service Unavailable)</description></item>
    /// <item><description>Timeout → 504 (Gateway Timeout)</description></item>
    /// </list>
    /// </remarks>
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
            { BenzeneResultStatus.TooManyRequests, "429"},
            { BenzeneResultStatus.UnexpectedError, "500"},
            { BenzeneResultStatus.NotImplemented, "501"},
            { BenzeneResultStatus.ServiceUnavailable, "503"},
            { BenzeneResultStatus.Timeout, "504"}
        };
    }

    /// <summary>
    /// Maps a Benzene result status to an HTTP status code.
    /// </summary>
    /// <param name="benzeneResultStatus">The Benzene result status string to map.</param>
    /// <returns>
    /// The corresponding HTTP status code as a string, or "500" if the status is null
    /// or not recognized.
    /// </returns>
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

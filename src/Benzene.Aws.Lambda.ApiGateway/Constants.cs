namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Provides constants used across the API Gateway package.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The <c>content-type</c> response header name.
    /// </summary>
    public const string ContentTypeHeader = "content-type";

    /// <summary>
    /// The default topic used for health check requests when none is specified.
    /// </summary>
    public const string DefaultHealthCheckTopic = "healthcheck";
}

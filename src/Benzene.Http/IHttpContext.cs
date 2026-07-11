namespace Benzene.Http;

/// <summary>
/// Represents the HTTP request and response context for processing HTTP requests in Benzene.
/// </summary>
/// <remarks>
/// This is a marker interface that serves as the base type for all HTTP context implementations.
/// Transport-specific implementations (such as ASP.NET Core, AWS Lambda, etc.) should implement
/// this interface and provide access to their underlying HTTP request and response objects.
/// </remarks>
public interface IHttpContext
{

}
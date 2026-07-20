namespace Benzene.CodeGen.ApiGateway
{
    /// <summary>
    /// Configures <see cref="ApiGatewayBuilderV1"/>. Everything that used to be hard-coded for one
    /// particular deployment (the custom authorizer name, the identity claims injected from the
    /// authorizer context, the public/unauthenticated topics, and the CORS allow-headers) is set here,
    /// so the generator is reusable. All defaults are generic: no custom authorizer, no injected
    /// identity headers, no unauthenticated topics, and a minimal allow-headers list.
    /// </summary>
    public class ApiGatewayOptions
    {
        /// <summary>Initializes a new instance.</summary>
        /// <param name="url">The backend integration URI token substituted into each operation's <c>uri</c>.</param>
        public ApiGatewayOptions(string url)
        {
            Url = url;
        }

        /// <summary>The backend integration URI token (emitted as <c>#{Url}#</c> for downstream substitution).</summary>
        public string Url { get; }

        /// <summary>
        /// The name of the API Gateway custom authorizer applied to secured operations (in addition to
        /// <c>api_key</c>). Null (the default) emits no custom authorizer - operations are secured by
        /// <c>api_key</c> only. Set this to your authorizer's name to require it.
        /// </summary>
        public string? AuthorizerName { get; set; }

        /// <summary>
        /// Topics whose operations are public - the custom <see cref="AuthorizerName"/> is not applied
        /// to them (they still carry <c>api_key</c>). Empty by default. Matched by exact topic id.
        /// </summary>
        public IReadOnlyCollection<string> UnauthenticatedTopics { get; set; } = Array.Empty<string>();

        /// <summary>
        /// The value of the CORS <c>Access-Control-Allow-Headers</c> response header. Defaults to a
        /// minimal generic set; add your own custom request headers (e.g. <c>X-Tenant-Id</c>) here.
        /// </summary>
        public string AllowedHeaders { get; set; } = "Authorization,Content-Type,X-Api-Key";

        /// <summary>
        /// Extra request headers injected into the Lambda integration request template, mapping a
        /// header name to a VTL value expression - typically identity claims pulled from the custom
        /// authorizer's context, e.g. <c>["x-user-id"] = "$context.authorizer.userid"</c>. Empty by
        /// default (only the transport headers Content-Type/CorrelationId/SourceIP/UserAgent are sent).
        /// </summary>
        public IDictionary<string, string> IdentityHeaders { get; set; } = new Dictionary<string, string>();
    }
}

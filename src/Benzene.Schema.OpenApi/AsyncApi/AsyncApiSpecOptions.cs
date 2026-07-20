namespace Benzene.Schema.OpenApi.AsyncApi;

/// <summary>
/// App-wide options for the generated AsyncAPI document. Register one (see
/// <c>Extensions.SetAsyncApiResponseTopicSuffix</c>) to override the defaults; when none is
/// registered, <see cref="AsyncApiDocumentBuilder"/>'s defaults apply.
/// </summary>
public class AsyncApiSpecOptions
{
    /// <summary>
    /// The suffix appended to a handled topic to name its reply channel's address
    /// (<c>&lt;topic&gt;:&lt;suffix&gt;</c>). Defaults to
    /// <see cref="AsyncApiDocumentBuilder.DefaultResponseTopicSuffix"/> (<c>response</c>), so a
    /// handler on <c>shipping:get-all</c> replies on <c>shipping:get-all:response</c>.
    /// </summary>
    public string ResponseTopicSuffix { get; set; } = AsyncApiDocumentBuilder.DefaultResponseTopicSuffix;
}

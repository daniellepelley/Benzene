using Benzene.Azure.Function.AspNet;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Benzene.Test.Azure.Function;

/// <summary>
/// Regression coverage for <see cref="AspNetMessageHeadersGetter"/>: header field names are
/// lower-cased for lookup stability, but VALUES must be preserved verbatim (they used to be
/// lower-cased too, which corrupts bearer tokens, correlation IDs, base64, etc.).
/// </summary>
public class AspNetMessageHeadersGetterTest
{
    private static AspNetContext ContextWithHeaders(params (string Key, string Value)[] headers)
    {
        var httpContext = new DefaultHttpContext();
        foreach (var (key, value) in headers)
        {
            httpContext.Request.Headers[key] = value;
        }

        return new AspNetContext(httpContext.Request);
    }

    [Fact]
    public void GetHeaders_PreservesCaseSensitiveValues_LowerCasingOnlyTheName()
    {
        var getter = new AspNetMessageHeadersGetter();

        var result = getter.GetHeaders(ContextWithHeaders(
            ("Authorization", "Bearer AbCdEf123=="),
            ("X-Correlation-Id", "Corr-XYZ")));

        // Name lower-cased for stability; value untouched.
        Assert.Equal("Bearer AbCdEf123==", result["authorization"]);
        // x-correlation-id is a well-known header mapped to "correlationId" - value still verbatim.
        Assert.Equal("Corr-XYZ", result["correlationid"]);
    }
}

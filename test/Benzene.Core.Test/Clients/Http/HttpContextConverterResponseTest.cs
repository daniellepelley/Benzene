using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Client.Http;
using Benzene.Clients;
using Benzene.Results;
using Xunit;

namespace Benzene.Test.Clients.Http;

public class HttpContextConverterResponseTest
{
    private static BenzeneClientContext<string, string> CreateClientContext() =>
        new(new BenzeneClientRequest<string>("topic", "message", new Dictionary<string, string>()));

    [Fact]
    public async Task MapResponseAsync_ErrorStatusWithNonJsonBody_DoesNotThrow_AndSurfacesTheStatus()
    {
        // A 500 carrying an HTML error page must not be deserialized as TResponse (that would throw a
        // serialization exception and mask the real status).
        var converter = new HttpContextConverter<string, string>("GET", "https://example.test/x");
        var context = new HttpSendMessageContext(new HttpRequestMessage())
        {
            Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("<html>error</html>", Encoding.UTF8, "text/html")
            }
        };

        var clientContext = CreateClientContext();
        await converter.MapResponseAsync(clientContext, context);

        Assert.NotEqual(BenzeneResultStatus.Ok, clientContext.Response.Status);
    }

    [Fact]
    public async Task MapResponseAsync_SuccessStatusWithJsonBody_DeserializesTheResponse()
    {
        var converter = new HttpContextConverter<string, string>("GET", "https://example.test/x");
        var context = new HttpSendMessageContext(new HttpRequestMessage())
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("\"hello\"", Encoding.UTF8, "application/json")
            }
        };

        var clientContext = CreateClientContext();
        await converter.MapResponseAsync(clientContext, context);

        Assert.Equal(BenzeneResultStatus.Ok, clientContext.Response.Status);
        Assert.Equal("hello", clientContext.Response.Payload);
    }
}

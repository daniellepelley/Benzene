namespace Benzene.SelfHost.Http;

public static class HttpRequestParser
{
    public static HttpRequest Parse(string request)
    {
        var lines = request.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var requestLine = lines[0].Split(' ');

        var uriParts = requestLine[1].Split('?');
        var queryParameters = GetQueryParameters(uriParts);

        var httpRequest = new HttpRequest
        {
            Method = requestLine[0],
            Path = uriParts[0],
            QueryParameters = queryParameters
        };
        var emptyLineIndex = Array.IndexOf(lines, "");
        for (var i = 1; i < emptyLineIndex; i++)
        {
            var headerParts = lines[i].Split(new[] { ": " }, StringSplitOptions.None);
            httpRequest.Headers[headerParts[0]] = headerParts[1];
        }
        httpRequest.Body = string.Join(Environment.NewLine, lines.Skip(emptyLineIndex + 1));
        return httpRequest;
    }

    private static IDictionary<string, string> GetQueryParameters(string[] uriParts)
    {
        var output = new Dictionary<string, string>();
        if (uriParts.Length > 1)
        {
            var queryParams = uriParts[1].Split('&');
            foreach (var param in queryParams)
            {
                var paramParts = param.Split('=');
                if (paramParts.Length == 2)
                {
                    output[paramParts[0]] = paramParts[1];
                }
            }
        }

        return output;
    }
}

public static class HttpResponseParser
{
    public static HttpResponse Parse(string response)
    {
        var lines = response.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var statusLine = lines[0].Split(' ');
        var httpResponse = new HttpResponse
        {
            Version = statusLine[0],
            StatusCode = int.Parse(statusLine[1]),
            ReasonPhrase = string.Join(' ', statusLine.Skip(2))
        };
        var emptyLineIndex = Array.IndexOf(lines, "");
        for (var i = 1; i < emptyLineIndex; i++)
        {
            var headerParts = lines[i].Split(new[] { ": " }, StringSplitOptions.None);
            httpResponse.Headers[headerParts[0]] = headerParts[1];
        }
        httpResponse.Body = string.Join(Environment.NewLine, lines.Skip(emptyLineIndex + 1));
        return httpResponse;
    }
}


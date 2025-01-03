﻿using Benzene.Http;
using Microsoft.AspNetCore.Mvc;
using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;

namespace Benzene.Azure.AspNet;

public class AspNetContext : IHttpContext
{
    public AspNetContext(HttpRequest httpRequest)
    {
        HttpRequest = httpRequest;
    }

    public HttpRequest HttpRequest { get; }
    public ContentResult? ContentResult { get; set; }
}

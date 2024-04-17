using System.Globalization;
using System.Text;
using Benzene.CodeGen.Core;
using Benzene.Schema.OpenApi.EventService;
using Microsoft.AspNetCore.Routing.Template;

namespace Benzene.CodeGen.ApiGateway
{
    public class ApiGatewayBuilderV1 : ICodeBuilder<EventServiceDocument>
    {
        private readonly string _url;

        public ApiGatewayBuilderV1(string url)
        {
            _url = url;
        }
        
        public ICodeFile[] BuildCodeFiles(EventServiceDocument source)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"# AUTOGEN START {_url.ToUpperInvariant()}");
            stringBuilder.AppendLine("");

            var paths = source.Requests
                .Where(x => x.HttpMappings != null)
                .SelectMany(request => request.HttpMappings.Select(http => new { request, http }))
                .GroupBy(x => x.http.Path)
                .ToArray();


            foreach (var route in paths)
            {
                stringBuilder.Append(BuildPath(route.Key, route.Select(x => (x.http.Method, x.request.Topic)).ToArray()));
            }
            
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("# AUTOGEN END");
            

            return new ICodeFile[] {
                new CodeFile("openApi.yaml", stringBuilder.ToString().ToLines())
            };
        }


        public string BuildPath(string path, (string, string)[] endpoints)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($@"  /{path}:");
            stringBuilder.Append(BuildOptions(endpoints.Select(x => x.Item1).ToArray(), path, endpoints[0].Item2));

            foreach (var endpoint in endpoints)
            {
                stringBuilder.Append(BuildVerb(endpoint.Item1, path, endpoint.Item2));
            }

            return stringBuilder.ToString();
        }

        public string BuildOptions(string[] verbs, string path, string topic)
        {
            var routeTemplate = TemplateParser.Parse(path);
            var tag = CreateTag(path);
            var verbsText = string.Join(',', verbs.Select(x => x.ToUpperInvariant()));

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(@"    options:");
            stringBuilder.AppendLine(@"      tags:");
            stringBuilder.AppendLine($@"        - {tag}");
            BuildParameters(routeTemplate, stringBuilder);
            stringBuilder.AppendLine(@"      responses:");
            stringBuilder.AppendLine(@"        ""200"":");
            stringBuilder.AppendLine(@"          $ref: ""#/components/responses/corsResponse""");
            stringBuilder.AppendLine(@"      x-amazon-apigateway-integration:");
            stringBuilder.AppendLine(@"        responses:");
            stringBuilder.AppendLine(@"          default:");
            stringBuilder.AppendLine(@"            statusCode: ""200""");
            stringBuilder.AppendLine(@"            responseParameters:");
            stringBuilder.AppendLine(@"              method.response.header.Cache-Control: ""'no-store, no-cache'""");
            stringBuilder.AppendLine(@"              method.response.header.X-Frame-Options: ""'deny'""");
            stringBuilder.AppendLine(@"              method.response.header.X-XSS-Protection: ""'1; mode=block'""");
            stringBuilder.AppendLine(@"              method.response.header.Strict-Transport-Security: ""'max-age=31536000'""");
            stringBuilder.AppendLine(@"              method.response.header.Content-Security-Policy: ""'default-src \\'none\\';'""");
            stringBuilder.AppendLine(@"              method.response.header.X-Content-Type-Options: ""'nosniff'""");
            stringBuilder.AppendLine(@"              method.response.header.Referrer-Policy: ""'no-referrer'""");
            stringBuilder.AppendLine(@$"              method.response.header.Access-Control-Allow-Methods: ""'{verbsText},OPTIONS'""");
            stringBuilder.AppendLine(@"              method.response.header.Access-Control-Allow-Headers: ""'X-Query-Id,X-Tenant-Id,Authorization,Content-Type,X-Api-Key'""");
            stringBuilder.AppendLine(@"            responseTemplates:");
            stringBuilder.AppendLine(@"              application/json: |");
            CorsHeaders(stringBuilder);
            stringBuilder.AppendLine(@"        passthroughBehavior: when_no_match");
            stringBuilder.AppendLine(@"        requestTemplates:");
            stringBuilder.AppendLine(@"          application/json: '{""statusCode"": 200}'");
            stringBuilder.AppendLine(@"        type: mock");
            return stringBuilder.ToString();
        }

        private static void CorsHeaders(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine(@"                #set($domainWhitelist = [#cors_allowed_origins#])");
            stringBuilder.AppendLine(@"");
            stringBuilder.AppendLine(@"                #if (#cors_localhost#)");
            stringBuilder.AppendLine(@"                  #set($context.responseOverride.header.Access-Control-Allow-Origin = $input.params(""Origin""))");
            stringBuilder.AppendLine(@"                #elseif ($domainWhitelist.contains($input.params(""Origin"")))");
            stringBuilder.AppendLine(
                @"                  #set($context.responseOverride.header.Access-Control-Allow-Origin = $input.params(""Origin""))");
            stringBuilder.AppendLine(@"                #else");
            stringBuilder.AppendLine(@"                  #set($context.responseOverride.status = 405)");
            stringBuilder.AppendLine(@"                #end");
        }

        public string BuildVerb(string verb, string path, string topic)
        {
            var routeTemplate = TemplateParser.Parse(path);

            var parts = routeTemplate.Segments
                .Select(x => string.Join("", x.Parts.Select(x1 => x1.Text?.Replace("/", ""))))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

            var resource = "/" + string.Join("/", parts.TakeWhile(x => !x.Contains("{"))) + "/";

            var tag = CreateTag(path);

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($@"    {verb.ToLowerInvariant()}:");
            stringBuilder.AppendLine($@"      summary: ""{topic}""");
            stringBuilder.AppendLine(@"      tags:");
            stringBuilder.AppendLine($@"        - {tag}");

            BuildParameters(routeTemplate, stringBuilder);

            AddResponses(stringBuilder);
            stringBuilder.AppendLine(@"      security:");
            if (!OptionSecurityExclusion.excludeTopic.Contains(topic))
            {stringBuilder.AppendLine(@"        - OktaElementsCustomAuthoriser: []");}
            stringBuilder.AppendLine(@"        - api_key: []");
            stringBuilder.AppendLine(@"      x-amazon-apigateway-integration:");
            stringBuilder.AppendLine(@"        type: ""AWS""");
            stringBuilder.AppendLine(@"        httpMethod: POST");
            stringBuilder.AppendLine(@$"        uri: ""#{_url}#""");
            stringBuilder.AppendLine(@"        passthroughBehavior: ""never""");
            stringBuilder.AppendLine(@"        requestTemplates:");
            stringBuilder.AppendLine(@"          application/json: |");
            stringBuilder.AppendLine(@"            {");
            stringBuilder.AppendLine($@"              ""httpMethod"": ""{verb.ToUpperInvariant()}"",");
            stringBuilder.AppendLine($@"              ""resource"": ""{resource}"",");
            stringBuilder.AppendLine($@"              ""path"": ""/{path}"",");
            stringBuilder.AppendLine(@"              ""queryStringParameters"": {");
            stringBuilder.AppendLine(@"                #foreach($param in $input.params().querystring.keySet())");
            stringBuilder.AppendLine(@"                ""$param"": ""$util.escapeJavaScript($input.params().querystring.get($param))"" #if($foreach.hasNext),#end");
            stringBuilder.AppendLine(@"");
            stringBuilder.AppendLine(@"                #end");
            stringBuilder.AppendLine(@"              },");
            stringBuilder.AppendLine(@"              ""pathParameters"": {");
            stringBuilder.AppendLine(@"                #foreach($param in $input.params().path.keySet())");
            stringBuilder.AppendLine(@"                ""$param"": ""$util.escapeJavaScript($input.params().path.get($param))"" #if($foreach.hasNext),#end");
            stringBuilder.AppendLine(@"");
            stringBuilder.AppendLine(@"                #end");
            stringBuilder.AppendLine(@"              },");
            stringBuilder.AppendLine(@"              ""headers"": {");
            stringBuilder.AppendLine(@"                ""Content-Type"": ""application/json"",");
            stringBuilder.AppendLine(@"                ""CorrelationId"": ""$context.requestId"",");
            stringBuilder.AppendLine(@"                ""SourceIP"": ""$context.identity.sourceIp"",");
            stringBuilder.AppendLine(@"                ""UserAgent"": ""$context.identity.userAgent"",");
            stringBuilder.AppendLine(@"                ""x-tenant-id"":""$context.authorizer.tenantid"",");
            stringBuilder.AppendLine(@"                ""PlatformTenantId"":""$context.authorizer.tenantid"",");
            stringBuilder.AppendLine(@"                ""x-user-id"":""$context.authorizer.userid"",");
            stringBuilder.AppendLine(@"                ""x-permissions"":""$context.authorizer.permissions"",");
            stringBuilder.AppendLine(@"                ""x-licenses"":""$context.authorizer.licenses"",");
            stringBuilder.AppendLine(@"                ""x-subscriptions"":""$context.authorizer.subscriptions""");
            stringBuilder.AppendLine(@"              },");
            stringBuilder.AppendLine(@"              ""requestContext"": {");
            stringBuilder.AppendLine(@"                ""domainName"": ""$context.domainName""");
            stringBuilder.AppendLine(@"              },");
            stringBuilder.AppendLine(@"              ""body"": ""$util.escapeJavaScript($input.json('$'))""");
            stringBuilder.AppendLine(@"            }");
            stringBuilder.AppendLine(@"        responses:");
            stringBuilder.AppendLine(@"          default:");
            stringBuilder.AppendLine(@"            statusCode: ""200""");
            stringBuilder.AppendLine(@"            responseParameters:");
            stringBuilder.AppendLine(@$"              method.response.header.Access-Control-Allow-Methods: ""'{verb},OPTIONS'""");
            stringBuilder.AppendLine(@"              method.response.header.Access-Control-Allow-Headers: ""'X-Query-Id,X-Tenant-Id,Authorization,Content-Type,X-Api-Key'""");
            stringBuilder.AppendLine(@"              method.response.header.Cache-Control: ""'no-store, no-cache'""");
            stringBuilder.AppendLine(@"              method.response.header.Content-Security-Policy: ""'default-src \\'none\\';'""");
            stringBuilder.AppendLine(@"              method.response.header.Referrer-Policy: ""'no-referrer'""");
            stringBuilder.AppendLine(@"              method.response.header.Strict-Transport-Security: ""'max-age=31536000'""");
            stringBuilder.AppendLine(@"              method.response.header.X-Content-Type-Options: ""'nosniff'""");
            stringBuilder.AppendLine(@"              method.response.header.X-Frame-Options: ""'deny'""");
            stringBuilder.AppendLine(@"              method.response.header.X-XSS-Protection: ""'1; mode=block'""");
            stringBuilder.AppendLine(@"            responseTemplates:");
            stringBuilder.AppendLine(@"              application/json: |");
            stringBuilder.AppendLine(@"                #set($context.responseOverride.status = $input.path('$.statusCode'))");
            stringBuilder.AppendLine(@"                $input.path('$.body')");
            stringBuilder.AppendLine(@"");
            CorsHeaders(stringBuilder);

            stringBuilder.AppendLine(@"");
            return stringBuilder.ToString();
        }

        private static void AddResponses(StringBuilder stringBuilder)
        {
            var responsesDictionary = new Dictionary<string, string>
            {
                { "200", "200OkEmpty" },
                { "201", "201Created" },
                { "204", "204NoContent" },
                { "400", "400BadRequest" },
                { "401", "401Unauthorised" },
                { "403", "403Forbidden" },
                { "404", "404NotFound" },
                { "422", "422UnprocessableEntity" },
                { "500", "500InternalServerError" },
                { "503", "503ServiceUnavailable" }
            };

            stringBuilder.AppendLine(@"      responses:");

            foreach (var response in responsesDictionary)
            {
                stringBuilder.AppendLine(@$"        ""{response.Key}"":");
                stringBuilder.AppendLine(@$"          $ref: ""#/components/responses/{response.Value}""");
            }
        }

        private static void BuildParameters(RouteTemplate routeTemplate, StringBuilder stringBuilder)
        {
            if (routeTemplate.Parameters.Any())
            {
                stringBuilder.AppendLine(@"      parameters:");
                foreach (var parameter in routeTemplate.Parameters)
                {
                    stringBuilder.AppendLine($@"      - name: {parameter.Name}");
                    stringBuilder.AppendLine(@"        in: path");
                    stringBuilder.AppendLine(@"        required: true");
                    stringBuilder.AppendLine(@"        schema:");
                    stringBuilder.AppendLine(@"          type: string");
                }
            }
        }

        private string CreateTag(string path)
        {
            var parts = path
                .Split('/')
                .Where(x => !x.StartsWith("{"))
                .Select(x => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(x))
                .ToArray();

            return string.Join(' ', parts);
        }

    }
}

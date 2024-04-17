using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Benzene.Schema.OpenApi.EventService;

public class EventServiceDocumentDeserializer
{
    private readonly EventServiceDocumentBuilder _eventServiceDocumentBuilder = new();
    private readonly OpenApiStringReader _openApiStringReader = new();
    
    public EventServiceDocument Deserialize(string json)
    {
        var jObject = JObject.Parse(json);

        AddSchema(jObject);

        var doc = _eventServiceDocumentBuilder.Build();
        doc.Info = GetInfo(jObject);
        doc.Tags = GetTags(jObject);
        doc.Events = GetEvents(jObject);
        doc.Requests = GetRequests(jObject);
        return doc;
    }

    private void AddSchema(JObject jObject)
    {
        var schemaJToken = jObject[OpenApiConstants.Components][OpenApiConstants.Schemas];

        if (schemaJToken == null)
        {
            return;
        }
        
        foreach (var x in schemaJToken)
        {
            var p = (JProperty)x;
            var schema =
                _openApiStringReader.ReadFragment<OpenApiSchema>(p.Value.ToString(), OpenApiSpecVersion.OpenApi3_0,
                    out _);
            var key = p.Path.Split('.').Last();
            _eventServiceDocumentBuilder.AddSchema(key, schema);
        }
    }

    private OpenApiInfo GetInfo(JObject jObject)
    {
        var jToken = jObject[OpenApiConstants.Info];
        return _openApiStringReader.ReadFragment<OpenApiInfo>(jToken.ToString(), OpenApiSpecVersion.OpenApi3_0, out _);
    }

    private OpenApiTag[] GetTags(JObject jObject)
    {
        var reader = new OpenApiStringReader();
        var jArray = jObject[OpenApiConstants.Tags] as JArray;

        if (jArray == null)
        {
            return Array.Empty<OpenApiTag>();
        }
        
        return jArray!
            .Select(x => reader.ReadFragment<OpenApiTag>(x.ToString(), OpenApiSpecVersion.OpenApi3_0, out _))
            .ToArray();
    }

    private Event[] GetEvents(JObject jObject)
    {
        var eventsJArray = jObject.GetValue("events") as JArray;
        return eventsJArray!.Select(GetEvent)
            .ToArray();
    }

    private RequestResponse[] GetRequests(JObject jObject)
    {
        var requestsJArray = jObject.GetValue("requests") as JArray;
        return requestsJArray!.Select(GetRequest)
            .ToArray();
    }

    private Event GetEvent(JToken jToken)
    {
        var @event = JsonConvert.DeserializeObject<Event>(jToken.ToString(Formatting.Indented));

        return new Event(@event.Topic, GetSchema(jToken, "message"));
    }

    private RequestResponse GetRequest(JToken jToken)
    {
        var request = JsonConvert.DeserializeObject<RequestResponse>(jToken.ToString(Formatting.Indented));

        request!.Request = GetSchema(jToken, "request");
        request.Response = GetSchema(jToken, "response");
        
        return request;
    }

    private OpenApiSchema GetSchema(JToken jToken, string path)
    {
        var json = jToken[path].ToString();
        return _openApiStringReader.ReadFragment<OpenApiSchema>(json, OpenApiSpecVersion.OpenApi3_0, out _);
    }
}

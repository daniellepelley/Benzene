namespace Benzene.Schema.OpenApi;

public class SpecRequest
{
    public SpecRequest()
    {
        
    }
    public SpecRequest(string type, string format)
    {
        Format = format;
        Type = type;
    }

    public string Type { get; set; }
    public string Format { get; set; }
}

namespace Benzene.Schema.OpenApi;

public class SpecRequest
{
    public SpecRequest(string type, string format)
    {
        Format = format;
        Type = type;
    }

    public string Type { get; }
    public string Format { get; }
}

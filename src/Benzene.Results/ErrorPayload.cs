namespace Benzene.Results;

public class ErrorPayload
{
    public ErrorPayload()
    {
        
    }

    public ErrorPayload(string status, string[] errors)
    {
        Status = status;
        Errors = errors;
    }

    public string Status { get; set; }

    public string[] Errors { get; set; }
}

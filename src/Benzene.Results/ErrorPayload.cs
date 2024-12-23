namespace Benzene.Results;

public class ErrorPayload : ProblemDetails
{
    public ErrorPayload()
    {
        
    }
    
    public ErrorPayload(string status, string[] errors)
    {
        Status = status;
        Detail = string.Join(", ", errors);
    }
}

public class ProblemDetails
{
    public string Type { get; set; }
    public string Status { get; set; }
    public string Title { get; set; }
    public string Detail { get; set; }
    public string Instance { get; set; }
}

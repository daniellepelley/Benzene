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
namespace Benzene.HealthChecks.Core;

public class HealthCheckResult : IHealthCheckResult
{
    public const string UnknownType = "Unknown";
    
    public static IHealthCheckResult CreateInstance(bool success)
    {
        return CreateInstance(success, UnknownType, new Dictionary<string, object>());
    }
    
    public static IHealthCheckResult CreateInstance(bool success, string type)
    {
        return CreateInstance(success, type, new Dictionary<string, object>());
    }
    
    public static Task<IHealthCheckResult> CreateInstance(Task<bool> success, string type)
    {
        return success.ContinueWith(x => CreateInstance(x.Result, type, new Dictionary<string, object>()));
    }

    public static IHealthCheckResult CreateInstance(bool success, string type, IDictionary<string, object> data)
    {
        return new HealthCheckResult(success ? HealthCheckStatus.Ok : HealthCheckStatus.Failed, type, data);
    }

    public static IHealthCheckResult CreateWarning(string type)
    {
        return new HealthCheckResult(HealthCheckStatus.Warning, type, new Dictionary<string, object>());
    }
    
    public static IHealthCheckResult CreateWarning(string type, IDictionary<string, object> data)
    {
        return new HealthCheckResult(HealthCheckStatus.Warning, type, data);
    }

    public HealthCheckResult(string status, string type, IDictionary<string, object> data)
    {
        Status = status;
        Type = type;
        Data = data;
    }

    public string Status { get; }

    public string Type { get; } 

    public IDictionary<string, object> Data { get; }
}
